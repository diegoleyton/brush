using System.Collections.Generic;
using UnityEngine;

namespace Flowbit.Utilities.Audio
{
    /// <summary>
    /// Provides a fixed pool of AudioSource instances for looped playback.
    /// Each active loop is tracked by an integer audio identifier.
    /// </summary>
    public sealed class LoopingAudioSourcePool : MonoBehaviour, ILoopingAudioSourcePool
    {
        [SerializeField] private AudioSource _audioSourceTemplate;
        [SerializeField][Min(1)] private int _poolSize = 2;

        private readonly List<AudioSource> _audioSources = new();
        private readonly Dictionary<int, AudioSource> _sourcesByAudioId = new();
        private readonly Dictionary<AudioSource, int> _audioIdsBySource = new();
        private bool _isInitialized;

        /// <summary>
        /// Returns an AudioSource assigned to the given audio identifier.
        /// If no source is assigned yet, one is allocated.
        /// </summary>
        /// <param name="audioId">The integer identifier of the looped sound.</param>
        /// <returns>An AudioSource assigned to the given identifier.</returns>
        public AudioSource GetOrCreateSource(int audioId)
        {
            EnsureInitialized();

            if (_sourcesByAudioId.TryGetValue(audioId, out AudioSource existingSource))
            {
                return existingSource;
            }

            AudioSource source = GetUnusedSource();
            if (source == null)
            {
                source = StealOldestSource();
            }

            Assign(audioId, source);
            return source;
        }

        /// <summary>
        /// Tries to retrieve the AudioSource assigned to the given audio identifier.
        /// </summary>
        /// <param name="audioId">The integer identifier of the looped sound.</param>
        /// <param name="audioSource">The assigned AudioSource when found.</param>
        /// <returns>True if a source is assigned; otherwise false.</returns>
        public bool TryGetSource(int audioId, out AudioSource audioSource)
        {
            EnsureInitialized();
            return _sourcesByAudioId.TryGetValue(audioId, out audioSource);
        }

        /// <summary>
        /// Releases the source associated with the given audio identifier.
        /// </summary>
        /// <param name="audioId">The integer identifier of the looped sound.</param>
        public void ReleaseSource(int audioId)
        {
            EnsureInitialized();

            if (!_sourcesByAudioId.TryGetValue(audioId, out AudioSource source))
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.loop = false;

            _sourcesByAudioId.Remove(audioId);
            _audioIdsBySource.Remove(source);
        }

        /// <summary>
        /// Stops and releases all active looping sources.
        /// </summary>
        public void StopAndReleaseAll()
        {
            EnsureInitialized();

            foreach (AudioSource source in _audioSources)
            {
                source.Stop();
                source.clip = null;
                source.loop = false;
            }

            _sourcesByAudioId.Clear();
            _audioIdsBySource.Clear();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            _audioSources.Clear();
            _sourcesByAudioId.Clear();
            _audioIdsBySource.Clear();

            if (_audioSourceTemplate == null)
            {
                Debug.LogError($"LoopingAudioSourcePool on '{name}' requires an AudioSource template.", this);
                return;
            }

            for (int index = 0; index < _poolSize; index++)
            {
                AudioSource instance = CreateInstance(index);
                _audioSources.Add(instance);
            }

            _audioSourceTemplate.gameObject.SetActive(false);
        }

        private AudioSource CreateInstance(int index)
        {
            GameObject instanceObject = Instantiate(_audioSourceTemplate.gameObject, transform);
            instanceObject.name = $"{_audioSourceTemplate.gameObject.name}_Loop_{index}";
            instanceObject.SetActive(true);

            AudioSource instance = instanceObject.GetComponent<AudioSource>();
            instance.playOnAwake = false;
            instance.loop = false;

            return instance;
        }

        private AudioSource GetUnusedSource()
        {
            for (int index = 0; index < _audioSources.Count; index++)
            {
                AudioSource source = _audioSources[index];

                if (!_audioIdsBySource.ContainsKey(source))
                {
                    return source;
                }
            }

            return null;
        }

        private AudioSource StealOldestSource()
        {
            AudioSource source = _audioSources[0];

            if (_audioIdsBySource.TryGetValue(source, out int previousAudioId))
            {
                _sourcesByAudioId.Remove(previousAudioId);
                _audioIdsBySource.Remove(source);
            }

            source.Stop();
            source.clip = null;
            source.loop = false;

            return source;
        }

        private void Assign(int audioId, AudioSource source)
        {
            if (_audioIdsBySource.TryGetValue(source, out int previousAudioId))
            {
                _sourcesByAudioId.Remove(previousAudioId);
                _audioIdsBySource.Remove(source);
            }

            _sourcesByAudioId[audioId] = source;
            _audioIdsBySource[source] = audioId;
        }
    }
}