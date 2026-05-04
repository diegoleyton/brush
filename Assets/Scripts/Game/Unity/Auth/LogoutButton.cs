using System;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.ScreenBlocker;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Scenes;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Unity.Auth
{
    /// <summary>
    /// Reusable logout component that can be attached to any UI object and wired to a button.
    /// It can optionally navigate to the auth scene after clearing the session.
    /// </summary>
    public sealed class LogoutButton : MonoBehaviour
    {
        [SerializeField]
        private Button button_;

        [SerializeField]
        private bool requireConfirmation_ = true;

        [SerializeField]
        private bool navigateToAuthSceneAfterLogout_ = true;

        [SerializeField]
        private bool navigateAsRoot_ = true;

        private IAuthService authService_;
        private IGameNavigationService navigationService_;
        private ScreenBlocker screenBlocker_;
        private bool isBusy_;

        [Inject]
        public void Construct(
            IAuthService authService,
            IGameNavigationService navigationService,
            ScreenBlocker screenBlocker)
        {
            authService_ = authService;
            navigationService_ = navigationService;
            screenBlocker_ = screenBlocker;
        }

        private void Awake()
        {
            if (button_ != null)
            {
                button_.onClick.AddListener(Logout);
                RefreshButtonState();
            }
        }

        private void OnEnable()
        {
            RefreshButtonState();
        }

        private void OnDestroy()
        {
            if (button_ != null)
            {
                button_.onClick.RemoveListener(Logout);
            }
        }

        public async void Logout()
        {
            if (authService_ == null || isBusy_)
            {
                return;
            }

            if (requireConfirmation_ && navigationService_ != null)
            {
                navigationService_.Navigate(
                    SceneType.ConfirmPopup,
                    new ConfirmPopupParams(onAccept: LogoutConfirmed, onCancel: null));
                return;
            }

            await LogoutConfirmedAsync();
        }

        private async void LogoutConfirmed()
        {
            await LogoutConfirmedAsync();
        }

        private async System.Threading.Tasks.Task LogoutConfirmedAsync()
        {
            if (authService_ == null || isBusy_)
            {
                return;
            }

            isBusy_ = true;
            RefreshButtonState();
            IDisposable blockScope = screenBlocker_?.BlockScope(
                "Logout",
                showLoadingWithTime: true,
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.auth.logout",
                    "Signing out..."));

            try
            {
                await authService_.LogoutAsync();

                if (!navigateToAuthSceneAfterLogout_ || navigationService_ == null)
                {
                    return;
                }

                if (navigateAsRoot_)
                {
                    navigationService_.NavigateAsRoot(SceneType.AuthScene);
                }
                else
                {
                    navigationService_.Navigate(SceneType.AuthScene);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                blockScope?.Dispose();
                isBusy_ = false;
                RefreshButtonState();
            }
        }

        private void RefreshButtonState()
        {
            if (button_ == null)
            {
                return;
            }

            button_.interactable = !isBusy_;
        }
    }
}
