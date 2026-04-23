using UnityEngine;

namespace Flowbit.Utilities.Audio
{
    /// <summary>
    /// Provides AudioSource instances for looped playback identified by integer keys.
    /// </summary>
    public interface ILoopingAudioSourcePool
    {
        /// <summary>
        /// Returns an AudioSource assigned to the given audio identifier.
        /// If no source is assigned yet, one is allocated.
        /// </summary>
        /// <param name="audioId">The integer identifier of the looped sound.</param>
        /// <returns>An AudioSource assigned to the given identifier.</returns>
        AudioSource GetOrCreateSource(int audioId);

        /// <summary>
        /// Tries to retrieve the AudioSource assigned to the given audio identifier.
        /// </summary>
        /// <param name="audioId">The integer identifier of the looped sound.</param>
        /// <param name="audioSource">The assigned AudioSource when found.</param>
        /// <returns>True if a source is assigned; otherwise false.</returns>
        bool TryGetSource(int audioId, out AudioSource audioSource);

        /// <summary>
        /// Releases the source associated with the given audio identifier.
        /// </summary>
        /// <param name="audioId">The integer identifier of the looped sound.</param>
        void ReleaseSource(int audioId);

        /// <summary>
        /// Stops and releases all active looping sources.
        /// </summary>
        void StopAndReleaseAll();
    }
}