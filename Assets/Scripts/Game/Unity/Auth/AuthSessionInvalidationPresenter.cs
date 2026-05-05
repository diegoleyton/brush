using System;

using Flowbit.Utilities.Core.Events;

using Game.Core.Events;
using Game.Unity.Scenes;
using Game.Unity.Definitions;

using UnityEngine;

using Zenject;

namespace Game.Unity.Auth
{
    /// <summary>
    /// Presents a global session invalidation popup and exits the application when the player acknowledges it.
    /// </summary>
    public sealed class AuthSessionInvalidationPresenter : IInitializable, IDisposable
    {
        private readonly EventDispatcher dispatcher_;
        private readonly IGameNavigationService navigationService_;
        private bool isHandling_;

        public AuthSessionInvalidationPresenter(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService)
        {
            dispatcher_ = dispatcher;
            navigationService_ = navigationService;
        }

        public void Initialize()
        {
            dispatcher_?.Subscribe<AuthSessionInvalidatedEvent>(OnSessionInvalidated);
        }

        public void Dispose()
        {
            dispatcher_?.Unsubscribe<AuthSessionInvalidatedEvent>(OnSessionInvalidated);
        }

        private void OnSessionInvalidated(AuthSessionInvalidatedEvent eventData)
        {
            if (isHandling_ || navigationService_ == null)
            {
                return;
            }

            isHandling_ = true;

            navigationService_.Navigate(
                SceneType.ErrorPopup,
                new ErrorPopupParams(
                    eventData?.Message ?? "Your session is no longer valid. Please log in again.",
                    title: "Session ended",
                    onClose: HandlePopupClosed));
        }

        private void HandlePopupClosed()
        {
            isHandling_ = false;
            Application.Quit();
        }
    }
}
