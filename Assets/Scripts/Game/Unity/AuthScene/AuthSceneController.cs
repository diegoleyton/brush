using System;
using System.Threading.Tasks;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.Unity.UI;
using Game.Core.Services;
using Game.Unity.Auth;
using Game.Unity.Definitions;
using Game.Unity.Scenes;
using Game.Unity.UI;
using UnityEngine;
using Zenject;

namespace Game.Unity.AuthScene
{
    /// <summary>
    /// Scene entry point for authentication flows.
    /// Owns post-auth routing so the auth view stays focused on inputs and feedback.
    /// </summary>
    public sealed class AuthSceneController : SceneBase
    {
        [SerializeField]
        private AuthPanel authPanel_;

        private IAuthService authService_;
        private IProfilesService profilesService_;
        private IChildGameStateSyncService childGameStateSyncService_;
        private ClientGameStateStore gameStateStore_;
        private ScreenBlocker screenBlocker_;
        private bool isRedirecting_;

        [Inject]
        public void Construct(
            Flowbit.Utilities.Core.Events.EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            IAuthService authService,
            IProfilesService profilesService,
            IChildGameStateSyncService childGameStateSyncService,
            ClientGameStateStore gameStateStore,
            ScreenBlocker screenBlocker)
        {
            base.Construct(dispatcher, navigationService);
            authService_ = authService;
            profilesService_ = profilesService;
            childGameStateSyncService_ = childGameStateSyncService;
            gameStateStore_ = gameStateStore;
            screenBlocker_ = screenBlocker;
        }

        private void Reset()
        {
            sceneType_ = SceneType.AuthScene;
        }

        protected override void Initialize()
        {
            if (authPanel_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(AuthSceneController)} requires a {nameof(AuthPanel)} reference.");
            }

            authPanel_.AuthSucceeded += OnAuthSucceeded;
            _ = TryAutoRedirectAsync();
        }

        private void OnDestroy()
        {
            if (authPanel_ != null)
            {
                authPanel_.AuthSucceeded -= OnAuthSucceeded;
            }
        }

        private async Task TryAutoRedirectAsync()
        {
            if (authService_ == null)
            {
                return;
            }

            try
            {
                await authService_.InitializeAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            await RedirectIfAuthenticatedAsync();
        }

        private async void OnAuthSucceeded(AuthResult _)
        {
            await RedirectIfAuthenticatedAsync();
        }

        private async Task RedirectIfAuthenticatedAsync()
        {
            if (isRedirecting_ || authService_ == null || profilesService_ == null || gameStateStore_ == null)
            {
                return;
            }

            if (!authService_.HasSession)
            {
                return;
            }

            isRedirecting_ = true;
            IDisposable blockScope = screenBlocker_?.BlockScope(
                "AuthRedirect",
                showLoadingWithTime: true,
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.auth.redirect",
                    "Loading profile..."));
            try
            {
                await profilesService_.RefreshAsync();

                if (gameStateStore_.AllProfiles.Count == 0)
                {
                    NavigationService.NavigateAsRoot(SceneType.ProfileManagementScene);
                    return;
                }

                if (childGameStateSyncService_ != null)
                {
                    await childGameStateSyncService_.EnsureCurrentProfileLoadedAsync();
                }

                if (gameStateStore_.AllProfiles.Count > 1)
                {
                    NavigationService.NavigateAsRoot(SceneType.ProfileSelectionScene);
                    return;
                }

                NavigationService.NavigateAsRoot(SceneType.RoomScene);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                AuthPanel authPanel = authPanel_;
                if (authPanel != null)
                {
                    FeedbackMessage feedbackMessage = authPanel.GetComponent<FeedbackMessage>();
                    feedbackMessage?.ShowError(
                        RemoteOperationMessageFormatter.Format(
                            exception,
                            LocalizationServiceLocator.GetText("auth.redirect.error", "Could not load account.")),
                        durationSeconds: 3f);
                }
            }
            finally
            {
                blockScope?.Dispose();
                isRedirecting_ = false;
            }
        }
    }
}
