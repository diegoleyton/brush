using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Flowbit.Utilities.Coroutines;

namespace Flowbit.Utilities.Audio
{
    /// <summary>
    /// Plays audio from a typed sound library using one-shot and looping source pools.
    /// Supports crossfading between looping sounds.
    /// </summary>
    /// <typeparam name="TKey">The enum type used by the sound library.</typeparam>
    public sealed class AudioPlayer<TKey> where TKey : Enum
    {
        private readonly SoundLibrary<TKey> _soundLibrary;
        private readonly IAudioSourcePool _oneShotPool;
        private readonly ILoopingAudioSourcePool _loopingPool;
        private readonly ICoroutineService _coroutineService;
        private readonly Dictionary<int, float> _loopBaseVolumes = new();
        private readonly Dictionary<int, float> _loopVolumeMultipliers = new();

        private Coroutine _loopTransitionCoroutine;
        private LoopTransition _activeLoopTransition;

        /// <summary>
        /// Creates a new audio player.
        /// </summary>
        /// <param name="soundLibrary">The sound library used to resolve audio keys.</param>
        /// <param name="oneShotPool">The pool used for one-shot playback.</param>
        /// <param name="loopingPool">The pool used for looping playback.</param>
        /// <param name="coroutineService">The coroutine service used to run transitions.</param>
        public AudioPlayer(
            SoundLibrary<TKey> soundLibrary,
            IAudioSourcePool oneShotPool,
            ILoopingAudioSourcePool loopingPool,
            ICoroutineService coroutineService)
        {
            _soundLibrary = soundLibrary ?? throw new ArgumentNullException(nameof(soundLibrary));
            _oneShotPool = oneShotPool ?? throw new ArgumentNullException(nameof(oneShotPool));
            _loopingPool = loopingPool ?? throw new ArgumentNullException(nameof(loopingPool));
            _coroutineService = coroutineService ?? throw new ArgumentNullException(nameof(coroutineService));
        }

        /// <summary>
        /// Tries to play the sound associated with the given key as a one-shot.
        /// </summary>
        /// <param name="key">The sound key to play.</param>
        /// <returns>True if playback was triggered; otherwise false.</returns>
        public bool TryPlayOneShot(TKey key)
        {
            if (!TryGetPlaybackData(
                key,
                out SoundAsset _,
                out AudioClip clip,
                out float volume,
                out float pitch))
            {
                return false;
            }

            PrepareClip(clip);

            AudioSource source = _oneShotPool.GetAvailableSource();
            if (source == null)
            {
                return false;
            }

            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
            return true;
        }

        /// <summary>
        /// Tries to play the sound associated with the given key as a loop.
        /// If the same loop is already active, its playback settings are refreshed.
        /// </summary>
        /// <param name="key">The sound key to play in loop.</param>
        /// <returns>True if playback was triggered; otherwise false.</returns>
        public bool TryPlayLoop(TKey key)
        {
            CancelActiveLoopTransition();

            if (!TryGetPlaybackData(
                key,
                out SoundAsset _,
                out AudioClip clip,
                out float volume,
                out float pitch))
            {
                return false;
            }

            PrepareClip(clip);

            int audioId = Convert.ToInt32(key);
            _loopBaseVolumes[audioId] = volume;
            AudioSource source = _loopingPool.GetOrCreateSource(audioId);
            if (source == null)
            {
                return false;
            }

            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.volume = GetScaledLoopVolume(audioId, volume);
            source.pitch = pitch;
            source.Play();

            return true;
        }

        /// <summary>
        /// Tries to transition from one active looping sound to another looping sound.
        /// The target loop starts at the equivalent playback position of the source loop.
        /// This only works if the source loop is currently playing.
        /// </summary>
        /// <param name="fromKey">The currently playing loop key.</param>
        /// <param name="toKey">The target loop key.</param>
        /// <param name="durationSeconds">The crossfade duration in seconds.</param>
        /// <returns>True if the transition started; otherwise false.</returns>
        public bool TryTransitionLoop(TKey fromKey, TKey toKey, float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                durationSeconds = 0.01f;
            }

            int fromAudioId = Convert.ToInt32(fromKey);
            int toAudioId = Convert.ToInt32(toKey);

            if (fromAudioId == toAudioId)
            {
                return false;
            }

            if (!_loopingPool.TryGetSource(fromAudioId, out AudioSource fromSource))
            {
                return false;
            }

            if (fromSource == null || !fromSource.isPlaying || !fromSource.loop || fromSource.clip == null)
            {
                return false;
            }

            if (!TryGetPlaybackData(
                toKey,
                out SoundAsset _,
                out AudioClip toClip,
                out float toVolume,
                out float toPitch))
            {
                return false;
            }

            PrepareClip(fromSource.clip);
            PrepareClip(toClip);

            CancelActiveLoopTransition();

            AudioSource toSource = _loopingPool.GetOrCreateSource(toAudioId);
            if (toSource == null)
            {
                return false;
            }

            if (ReferenceEquals(fromSource, toSource))
            {
                return false;
            }

            const double scheduleLeadTimeSeconds = 0.05d;
            _loopBaseVolumes[toAudioId] = toVolume;

            double dspNow = AudioSettings.dspTime;
            double dspStartTime = dspNow + scheduleLeadTimeSeconds;

            int toTimeSamples = GetMappedTimeSamplesAtDspTime(
                fromSource,
                toClip,
                dspStartTime,
                dspNow);

            toSource.Stop();
            toSource.clip = toClip;
            toSource.loop = true;
            toSource.pitch = toPitch;
            toSource.volume = 0f;
            toSource.timeSamples = toTimeSamples;
            toSource.PlayScheduled(dspStartTime);

            _activeLoopTransition = new LoopTransition(
                fromAudioId: fromAudioId,
                toAudioId: toAudioId,
                fromSource: fromSource,
                toSource: toSource,
                fromStartVolume: GetScaledLoopVolume(fromAudioId, GetLoopBaseVolume(fromAudioId, fromSource.volume)),
                toTargetVolume: GetScaledLoopVolume(toAudioId, toVolume),
                durationSeconds: durationSeconds,
                dspStartTime: dspStartTime);

            _loopTransitionCoroutine = _coroutineService.StartCoroutine(
                RunLoopTransition(_activeLoopTransition));

            return true;
        }

        /// <summary>
        /// Stops the active loop associated with the given key.
        /// </summary>
        /// <param name="key">The sound key to stop.</param>
        /// <returns>True if an active loop was found and stopped; otherwise false.</returns>
        public bool StopLoop(TKey key)
        {
            int audioId = Convert.ToInt32(key);

            if (_activeLoopTransition != null &&
                (_activeLoopTransition.FromAudioId == audioId || _activeLoopTransition.ToAudioId == audioId))
            {
                CancelActiveLoopTransition();
            }

            if (!_loopingPool.TryGetSource(audioId, out AudioSource _))
            {
                return false;
            }

            _loopingPool.ReleaseSource(audioId);
            ClearLoopState(audioId);
            return true;
        }

        /// <summary>
        /// Stops and releases all active loops.
        /// </summary>
        public void StopAllLoops()
        {
            CancelActiveLoopTransition();
            _loopingPool.StopAndReleaseAll();
            _loopBaseVolumes.Clear();
            _loopVolumeMultipliers.Clear();
        }

        /// <summary>
        /// Sets a volume multiplier for an active or future loop.
        /// </summary>
        /// <param name="key">The sound key to adjust.</param>
        /// <param name="multiplier">The non-negative volume multiplier.</param>
        /// <returns>True if the multiplier was applied or stored; otherwise false.</returns>
        public bool SetLoopVolumeMultiplier(TKey key, float multiplier)
        {
            int audioId = Convert.ToInt32(key);
            float clampedMultiplier = Mathf.Max(0f, multiplier);
            _loopVolumeMultipliers[audioId] = clampedMultiplier;

            bool updated = false;

            if (_activeLoopTransition != null)
            {
                if (_activeLoopTransition.FromAudioId == audioId)
                {
                    _activeLoopTransition.FromStartVolume =
                        GetScaledLoopVolume(audioId, GetLoopBaseVolume(audioId, _activeLoopTransition.FromSource.volume));
                    updated = true;
                }

                if (_activeLoopTransition.ToAudioId == audioId)
                {
                    _activeLoopTransition.ToTargetVolume =
                        GetScaledLoopVolume(audioId, GetLoopBaseVolume(audioId, _activeLoopTransition.ToTargetVolume));
                    updated = true;
                }
            }

            if (_loopingPool.TryGetSource(audioId, out AudioSource source) &&
                source != null &&
                _loopBaseVolumes.TryGetValue(audioId, out float baseVolume))
            {
                source.volume = GetScaledLoopVolume(audioId, baseVolume);
                updated = true;
            }

            return updated || _loopBaseVolumes.ContainsKey(audioId);
        }

        private IEnumerator RunLoopTransition(LoopTransition transition)
        {
            while (AudioSettings.dspTime < transition.DspStartTime)
            {
                if (!IsTransitionStillValid(transition))
                {
                    CleanupTransitionState();
                    yield break;
                }

                yield return null;
            }

            double fadeStartDspTime = transition.DspStartTime;
            double fadeEndDspTime = fadeStartDspTime + transition.DurationSeconds;

            while (AudioSettings.dspTime < fadeEndDspTime)
            {
                if (!IsTransitionStillValid(transition))
                {
                    CleanupTransitionState();
                    yield break;
                }

                double now = AudioSettings.dspTime;
                float t = Mathf.Clamp01((float)((now - fadeStartDspTime) / transition.DurationSeconds));

                transition.FromSource.volume = Mathf.Lerp(
                    transition.FromStartVolume,
                    0f,
                    t);

                transition.ToSource.volume = Mathf.Lerp(
                    0f,
                    transition.ToTargetVolume,
                    t);

                yield return null;
            }

            if (IsTransitionStillValid(transition))
            {
                transition.FromSource.volume = 0f;
                transition.ToSource.volume = transition.ToTargetVolume;
                _loopingPool.ReleaseSource(transition.FromAudioId);
                ClearLoopState(transition.FromAudioId);
            }

            CleanupTransitionState();
        }

        private bool IsTransitionStillValid(LoopTransition transition)
        {
            return transition != null &&
                   transition.FromSource != null &&
                   transition.ToSource != null &&
                   transition.FromSource.clip != null &&
                   transition.ToSource.clip != null;
        }

        private void CancelActiveLoopTransition()
        {
            if (_loopTransitionCoroutine != null)
            {
                _coroutineService.StopCoroutine(_loopTransitionCoroutine);
                _loopTransitionCoroutine = null;
            }

            if (_activeLoopTransition == null)
            {
                return;
            }

            if (_activeLoopTransition.ToSource != null)
            {
                _activeLoopTransition.ToSource.Stop();
            }

            if (_activeLoopTransition.FromSource != null)
            {
                _activeLoopTransition.FromSource.volume = _activeLoopTransition.FromStartVolume;
            }

            if (_activeLoopTransition.ToAudioId != _activeLoopTransition.FromAudioId)
            {
                _loopingPool.ReleaseSource(_activeLoopTransition.ToAudioId);
                ClearLoopState(_activeLoopTransition.ToAudioId);
            }

            _activeLoopTransition = null;
        }

        private void CleanupTransitionState()
        {
            _loopTransitionCoroutine = null;
            _activeLoopTransition = null;
        }

        private static void PrepareClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }
        }

        private static int GetMappedTimeSamplesAtDspTime(
            AudioSource fromSource,
            AudioClip toClip,
            double targetDspTime,
            double currentDspTime)
        {
            if (fromSource == null || fromSource.clip == null || toClip == null)
            {
                return 0;
            }

            AudioClip fromClip = fromSource.clip;

            if (fromClip.samples <= 0 || toClip.samples <= 0 || fromClip.frequency <= 0)
            {
                return 0;
            }

            int currentFromSamples = fromSource.timeSamples;
            double secondsUntilStart = Math.Max(0d, targetDspTime - currentDspTime);

            double advancedSamples =
                secondsUntilStart *
                fromClip.frequency *
                Mathf.Abs(fromSource.pitch);

            int predictedFromSamples = currentFromSamples + Mathf.RoundToInt((float)advancedSamples);

            predictedFromSamples %= fromClip.samples;
            if (predictedFromSamples < 0)
            {
                predictedFromSamples += fromClip.samples;
            }

            float normalizedTime = (float)predictedFromSamples / fromClip.samples;
            normalizedTime = Mathf.Repeat(normalizedTime, 1f);

            int mappedSamples = Mathf.RoundToInt(normalizedTime * toClip.samples);
            return Mathf.Clamp(mappedSamples, 0, Mathf.Max(0, toClip.samples - 1));
        }

        private bool TryGetPlaybackData(
            TKey key,
            out SoundAsset soundAsset,
            out AudioClip clip,
            out float volume,
            out float pitch)
        {
            clip = null;
            volume = 1f;
            pitch = 1f;

            if (!_soundLibrary.TryGetSoundAsset(key, out soundAsset))
            {
                return false;
            }

            if (soundAsset == null || !soundAsset.HasClips)
            {
                return false;
            }

            clip = soundAsset.GetRandomClip();
            if (clip == null)
            {
                return false;
            }

            volume = soundAsset.GetRandomVolume();
            pitch = soundAsset.GetRandomPitch();
            return true;
        }

        private void ClearLoopState(int audioId)
        {
            _loopBaseVolumes.Remove(audioId);
        }

        private float GetLoopBaseVolume(int audioId, float fallbackVolume)
        {
            if (_loopBaseVolumes.TryGetValue(audioId, out float baseVolume))
            {
                return baseVolume;
            }

            return fallbackVolume;
        }

        private float GetScaledLoopVolume(int audioId, float baseVolume)
        {
            float multiplier = 1f;

            if (_loopVolumeMultipliers.TryGetValue(audioId, out float configuredMultiplier))
            {
                multiplier = configuredMultiplier;
            }

            return Mathf.Max(0f, baseVolume * multiplier);
        }

        private sealed class LoopTransition
        {
            public int FromAudioId { get; }
            public int ToAudioId { get; }
            public AudioSource FromSource { get; }
            public AudioSource ToSource { get; }
            public float FromStartVolume { get; set; }
            public float ToTargetVolume { get; set; }
            public float DurationSeconds { get; }
            public double DspStartTime { get; }

            public LoopTransition(
                int fromAudioId,
                int toAudioId,
                AudioSource fromSource,
                AudioSource toSource,
                float fromStartVolume,
                float toTargetVolume,
                float durationSeconds,
                double dspStartTime)
            {
                FromAudioId = fromAudioId;
                ToAudioId = toAudioId;
                FromSource = fromSource;
                ToSource = toSource;
                FromStartVolume = fromStartVolume;
                ToTargetVolume = toTargetVolume;
                DurationSeconds = Mathf.Max(0.01f, durationSeconds);
                DspStartTime = dspStartTime;
            }
        }
    }
}
