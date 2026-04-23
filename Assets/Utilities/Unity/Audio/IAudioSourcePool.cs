using UnityEngine;

namespace Flowbit.Utilities.Audio
{
    /// <summary>
    /// Provides AudioSource instances for one-shot playback.
    /// </summary>
    public interface IAudioSourcePool
    {
        /// <summary>
        /// Returns an AudioSource that can be used immediately.
        /// </summary>
        /// <returns>An available AudioSource.</returns>
        AudioSource GetAvailableSource();
    }
}