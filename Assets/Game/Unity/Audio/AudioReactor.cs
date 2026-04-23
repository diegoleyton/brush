using System;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Audio;
using Game.Unity.Definitions;
using Game.Unity.Definitions.Events;

namespace Game.Unity.Audio
{
    /// <summary>
    /// Translates domain events into audio playback actions.
    /// </summary>
    public sealed class AudioReactor
    {
        private readonly EventDispatcher eventDispatcher_;
        private readonly AudioPlayer<SoundId> audioPlayer_;

        /// <summary>
        /// Creates a new audio reactor.
        /// </summary>
        public AudioReactor(
            EventDispatcher eventDispatcher,
            AudioPlayer<SoundId> audioPlayer)
        {
            eventDispatcher_ = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            audioPlayer_ = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            Subscribe();
        }

        private void Subscribe()
        {
            eventDispatcher_.Subscribe<OnNextScene>(OnNextScene);
            eventDispatcher_.Subscribe<OnPreviousScene>(OnPreviousScene);
            eventDispatcher_.Subscribe<OnPopupOpen>(_ => Play(SoundId.PopupOpen));
            eventDispatcher_.Subscribe<OnPopupClose>(_ => Play(SoundId.PopupOpen));
            eventDispatcher_.Subscribe<OnFirstScene>(_ => PlayLoop(SoundId.Main));
        }

        private void OnNextScene(OnNextScene e)
        {
            Play(SoundId.NextScene);
        }

        private void OnPreviousScene(OnPreviousScene e)
        {
            Play(SoundId.PreviousScene);
        }

        private void Play(SoundId soundId)
        {
            audioPlayer_.TryPlayOneShot(soundId);
        }

        private void PlayLoop(SoundId soundId)
        {
            audioPlayer_.StopAllLoops();
            audioPlayer_.TryPlayLoop(soundId);
        }
    }
}
