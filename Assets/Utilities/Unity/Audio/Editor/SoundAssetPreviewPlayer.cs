using UnityEngine;
using Flowbit.Utilities.Audio;

namespace Flowbit.Utilities.Unity.Audio
{
    /// <summary>
    /// Plays SoundAsset previews through a real AudioSource during Play Mode.
    /// </summary>
    public sealed class SoundAssetPreviewPlayer : MonoBehaviour
    {
        [SerializeField] private SoundAsset soundAsset_;
        [SerializeField] private AudioSource audioSource_;

        public SoundAsset SoundAsset => soundAsset_;
        public AudioSource AudioSource => audioSource_;

        private void Reset()
        {
            audioSource_ = GetComponent<AudioSource>();

            if (audioSource_ == null)
            {
                audioSource_ = gameObject.AddComponent<AudioSource>();
            }

            audioSource_.playOnAwake = false;
        }

        /// <summary>
        /// Plays a random preview using the configured SoundAsset.
        /// </summary>
        public void PlayRandomPreview()
        {
            if (soundAsset_ == null || audioSource_ == null)
            {
                return;
            }

            AudioClip clip = soundAsset_.GetRandomClip();
            if (clip == null)
            {
                return;
            }

            float volume = soundAsset_.GetRandomVolume();
            float pitch = soundAsset_.GetRandomPitch();

            audioSource_.Stop();

            audioSource_.clip = null;

            audioSource_.clip = clip;
            audioSource_.volume = volume;
            audioSource_.pitch = pitch;

            audioSource_.time = 0f;
            audioSource_.Play();

            Debug.Log($"Pitch: {pitch}");
        }

        /// <summary>
        /// Stops the current preview playback.
        /// </summary>
        public void StopPreview()
        {
            if (audioSource_ != null)
            {
                audioSource_.Stop();
            }
        }
    }
}