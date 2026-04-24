using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flowbit.Utilities.Audio
{
    /// <summary>
    /// Base generic sound library implemented as a ScriptableObject.
    /// Derive from this type with a concrete enum in each game.
    /// </summary>
    /// <typeparam name="TKey">The enum type used to identify sounds.</typeparam>
    public abstract class SoundLibrary<TKey> : ScriptableObject where TKey : Enum
    {
        [SerializeField] private SoundLibraryEntry<TKey>[] _entries = Array.Empty<SoundLibraryEntry<TKey>>();

        private Dictionary<TKey, SoundAsset> _cache;

        /// <summary>
        /// Gets the serialized entries contained in the library.
        /// </summary>
        public IReadOnlyList<SoundLibraryEntry<TKey>> Entries => _entries;

        /// <summary>
        /// Returns true when the library contains the given key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists in the library; otherwise false.</returns>
        public bool Contains(TKey key)
        {
            EnsureCache();
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// Tries to retrieve a sound asset associated with the given key.
        /// </summary>
        /// <param name="key">The key to resolve.</param>
        /// <param name="soundAsset">The resolved sound asset when found.</param>
        /// <returns>True if the key exists; otherwise false.</returns>
        public bool TryGetSoundAsset(TKey key, out SoundAsset soundAsset)
        {
            EnsureCache();
            return _cache.TryGetValue(key, out soundAsset);
        }

        /// <summary>
        /// Gets the sound asset associated with the given key.
        /// </summary>
        /// <param name="key">The key to resolve.</param>
        /// <returns>The associated sound asset.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the key is not present in the library.
        /// </exception>
        public SoundAsset GetSoundAsset(TKey key)
        {
            EnsureCache();

            if (_cache.TryGetValue(key, out SoundAsset soundAsset))
            {
                return soundAsset;
            }

            throw new KeyNotFoundException(
                $"No sound asset was found for key '{key}' in library '{name}'.");
        }

        private void OnEnable()
        {
            RebuildCache();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildCache();
        }
#endif

        private void EnsureCache()
        {
            if (_cache == null)
            {
                RebuildCache();
            }
        }

        private void RebuildCache()
        {
            _cache = new Dictionary<TKey, SoundAsset>();

            if (_entries == null)
            {
                return;
            }

            foreach (SoundLibraryEntry<TKey> entry in _entries)
            {
                if (entry == null || entry.SoundAsset == null)
                {
                    continue;
                }

                _cache[entry.Key] = entry.SoundAsset;
            }
        }
    }
}