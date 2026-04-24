using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flowbit.Utilities.Audio
{
    /// <summary>
    /// Provides a fixed pool of AudioSource instances for one-shot playback.
    /// </summary>
    public sealed class AudioSourcePool : MonoBehaviour, IAudioSourcePool
    {
        [SerializeField] private AudioSource _audioSourceTemplate;
        [SerializeField][Min(1)] private int _poolSize = 4;

        private readonly List<AudioSource> _audioSources = new();
        private bool _isInitialized;

        /// <summary>
        /// Returns an AudioSource that can be used immediately.
        /// If all sources are busy, one of them is reused.
        /// </summary>
        /// <returns>An available AudioSource.</returns>
        public AudioSource GetAvailableSource()
        {
            EnsureInitialized();

            for (int index = 0; index < _audioSources.Count; index++)
            {
                AudioSource audioSource = _audioSources[index];

                if (!audioSource.isPlaying)
                {
                    return audioSource;
                }
            }

            return _audioSources.Count > 0 ? _audioSources[0] : null;
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

            if (_audioSourceTemplate == null)
            {
                Debug.LogError($"AudioSourcePool on '{name}' requires an AudioSource template.", this);
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
            instanceObject.name = $"{_audioSourceTemplate.gameObject.name}_Sfx_{index}";
            instanceObject.SetActive(true);

            AudioSource instance = instanceObject.GetComponent<AudioSource>();
            instance.playOnAwake = false;
            instance.loop = false;

            return instance;
        }
    }
}