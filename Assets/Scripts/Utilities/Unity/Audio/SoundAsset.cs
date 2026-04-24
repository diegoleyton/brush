using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flowbit.Utilities.Audio
{
    /// <summary>
    /// Represents a reusable audio asset with clip variations
    /// and randomized playback settings.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundAsset", menuName = "Flowbit/Audio/Sound Asset")]
    public sealed class SoundAsset : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
        [SerializeField] private Vector2 _volumeRange = Vector2.one;
        [SerializeField] private Vector2 _pitchRange = Vector2.one;

        /// <summary>
        /// Gets the clips that can be used by this asset.
        /// </summary>
        public IReadOnlyList<AudioClip> Clips => _clips;

        /// <summary>
        /// Gets the randomized playback volume range for this asset.
        /// </summary>
        public Vector2 VolumeRange => _volumeRange;

        /// <summary>
        /// Gets the randomized playback pitch range for this asset.
        /// </summary>
        public Vector2 PitchRange => _pitchRange;

        /// <summary>
        /// Returns true when at least one valid clip is assigned.
        /// </summary>
        public bool HasClips => _clips != null && _clips.Length > 0;

        /// <summary>
        /// Returns a random clip from this asset.
        /// </summary>
        /// <returns>A valid clip, or null when no clips are available.</returns>
        public AudioClip GetRandomClip()
        {
            if (!HasClips)
            {
                return null;
            }

            if (_clips.Length == 1)
            {
                return _clips[0];
            }

            int index = UnityEngine.Random.Range(0, _clips.Length);
            return _clips[index];
        }

        /// <summary>
        /// Returns a randomized volume value using the configured range.
        /// </summary>
        /// <returns>A playback volume value.</returns>
        public float GetRandomVolume()
        {
            float min = Mathf.Min(_volumeRange.x, _volumeRange.y);
            float max = Mathf.Max(_volumeRange.x, _volumeRange.y);
            return UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// Returns a randomized pitch value using the configured range.
        /// </summary>
        /// <returns>A playback pitch value.</returns>
        public float GetRandomPitch()
        {
            float min = Mathf.Min(_pitchRange.x, _pitchRange.y);
            float max = Mathf.Max(_pitchRange.x, _pitchRange.y);
            return UnityEngine.Random.Range(min, max);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _volumeRange.x = Mathf.Max(0f, _volumeRange.x);
            _volumeRange.y = Mathf.Max(0f, _volumeRange.y);

            _pitchRange.x = Mathf.Max(0.01f, _pitchRange.x);
            _pitchRange.y = Mathf.Max(0.01f, _pitchRange.y);
        }
#endif
    }
}