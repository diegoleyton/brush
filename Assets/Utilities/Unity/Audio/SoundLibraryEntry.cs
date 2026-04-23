using System;
using UnityEngine;

namespace Flowbit.Utilities.Audio
{
    /// <summary>
    /// Represents a key-to-sound mapping inside a sound library.
    /// </summary>
    /// <typeparam name="TKey">The enum type used to identify sounds.</typeparam>
    [Serializable]
    public sealed class SoundLibraryEntry<TKey> where TKey : Enum
    {
        [SerializeField] private TKey _key;
        [SerializeField] private SoundAsset _soundAsset;

        /// <summary>
        /// Gets the key associated with this entry.
        /// </summary>
        public TKey Key => _key;

        /// <summary>
        /// Gets the sound asset associated with this entry.
        /// </summary>
        public SoundAsset SoundAsset => _soundAsset;
    }
}