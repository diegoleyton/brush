using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.ScreenBlocker;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Scenes;
using Game.Unity.UI;

using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.ProfileSelectionScene
{
    /// <summary>
    /// Presents the available save profiles and lets the player switch between them.
    /// </summary>
    public sealed class ProfileSelectionScene : RemoteBootstrapSceneBase
    {
        [SerializeField]
        private GameObject backButtonRoot_;

        [SerializeField]
        private RectTransform buttonListContainer_;

        [SerializeField]
        private ProfileSelectionButtonView profileButtonTemplate_;

        [SerializeField]
        private float selectionDelaySeconds_ = 0.2f;

        private readonly List<ProfileSelectionButtonView> spawnedButtons_ = new();

        private ClientGameStateStore gameStateStore_;
        private IProfilesService profilesService_;
        private IChildGameStateSyncService childGameStateSyncService_;
        private EventDispatcher dispatcher_;
        private bool dispatcherSubscribed_;
        private bool isLeavingScene_;
        private bool isPreparingSelectedProfile_;
        private bool hasLoadedRemoteProfiles_;
        private int pendingProfileIndex_ = -1;
        private Coroutine delayedExitCoroutine_;
        private IDisposable selectionDelayBlockScope_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            ClientGameStateStore gameStateStore,
            IProfilesService profilesService,
            IChildGameStateSyncService childGameStateSyncService,
            ScreenBlocker screenBlocker)
        {
            base.Construct(dispatcher, navigationService);
            dispatcher_ = dispatcher;
            gameStateStore_ = gameStateStore;
            profilesService_ = profilesService;
            childGameStateSyncService_ = childGameStateSyncService;
            ConfigureRemoteBootstrap(screenBlocker);
        }

        private void Awake()
        {
            sceneType_ = SceneType.ProfileSelectionScene;
            ValidateSerializedReferences();
        }

        private void OnEnable()
        {
            isLeavingScene_ = false;
            isPreparingSelectedProfile_ = false;
            pendingProfileIndex_ = -1;
            EnsureRemoteBootstrapEntryBlock();

            if (initialized_)
            {
                RefreshBackButtonVisibility();
                if (hasLoadedRemoteProfiles_)
                {
                    RefreshProfileButtons();
                }
                else
                {
                    ClearProfileButtons();
                }

                RefreshProfilesAsync();
            }
        }

        protected override void OnDestroy()
        {
            if (delayedExitCoroutine_ != null)
            {
                StopCoroutine(delayedExitCoroutine_);
                delayedExitCoroutine_ = null;
            }

            DisposeSelectionDelayBlockScope();
            UnsubscribeFromDispatcher();
            base.OnDestroy();
        }

        protected override void Initialize()
        {
            SubscribeToDispatcher();
            EnsureRemoteBootstrapEntryBlock();
            RefreshBackButtonVisibility();
            ClearProfileButtons();
            RefreshProfilesAsync();
        }

        public void GoBack()
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.Back();
                return;
            }

            NavigationService.Navigate(SceneType.RoomScene);
        }

        public void GoToProfileManagementScene()
        {
            NavigationService.Navigate(
                SceneType.PasswordPopup,
                new PasswordPopupParams(
                    onAccepted: () => NavigationService.Navigate(SceneType.ProfileManagementScene),
                    onCancelled: null));
        }

        private void OnProfileSwitched(ProfileSwitchedEvent _event)
        {
            if (!hasLoadedRemoteProfiles_)
            {
                ClearProfileButtons();
                return;
            }

            RefreshProfileButtons();

            if (pendingProfileIndex_ >= 0 && delayedExitCoroutine_ == null && !isPreparingSelectedProfile_)
            {
                _ = PrepareSelectedProfileAndExitAsync();
            }
        }

        private void OnProfileUpdated(ProfileUpdatedEvent _)
        {
            if (isLeavingScene_)
            {
                return;
            }

            RefreshBackButtonVisibility();
            if (hasLoadedRemoteProfiles_)
            {
                RefreshProfileButtons();
            }
            else
            {
                ClearProfileButtons();
            }
        }

        private void SubscribeToDispatcher()
        {
            if (dispatcherSubscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_.Subscribe<ProfileUpdatedEvent>(OnProfileUpdated);
            dispatcherSubscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!dispatcherSubscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_.Unsubscribe<ProfileUpdatedEvent>(OnProfileUpdated);
            dispatcherSubscribed_ = false;
        }

        private void RefreshProfileButtons()
        {
            if (buttonListContainer_ == null || gameStateStore_ == null)
            {
                return;
            }

            ClearProfileButtons();

            int currentProfileIndex = gameStateStore_.CurrentProfileIndex;

            for (int index = 0; index < gameStateStore_.AllProfiles.Count; index++)
            {
                Profile profile = gameStateStore_.AllProfiles[index];
                if (profile == null)
                {
                    continue;
                }

                int profileIndex = index;
                bool isSelectedProfile = profileIndex == currentProfileIndex;
                string label = profile.Name;

                ProfileSelectionButtonView buttonView =
                    Instantiate(profileButtonTemplate_, buttonListContainer_, false);
                buttonView.gameObject.name = $"ProfileButton_{profileIndex + 1}";
                buttonView.gameObject.SetActive(true);
                buttonView.Configure(label, isSelectedProfile, () => SelectProfile(profileIndex));
                spawnedButtons_.Add(buttonView);
            }
        }

        private void SelectProfile(int profileIndex)
        {
            if (gameStateStore_ == null ||
                profilesService_ == null ||
                !hasLoadedRemoteProfiles_ ||
                profileIndex < 0 ||
                profileIndex >= gameStateStore_.AllProfiles.Count)
            {
                if (!hasLoadedRemoteProfiles_)
                {
                    ShowBlockingErrorPopup(
                        LocalizationServiceLocator.GetText(
                            "profiles.load.required",
                            "Could not load profiles. Please try again."),
                        LocalizationServiceLocator.GetText(
                            "profiles.load.popup_title",
                            "Could not load profiles"));
                }

                return;
            }

            int currentProfileIndex = gameStateStore_.CurrentProfileIndex;
            if (currentProfileIndex == profileIndex)
            {
                if (!isPreparingSelectedProfile_)
                {
                    _ = PrepareSelectedProfileAndExitAsync();
                }

                return;
            }

            pendingProfileIndex_ = profileIndex;
            profilesService_.SelectProfile(profileIndex);
        }

        private void RefreshProfilesAsync()
        {
            RunRemoteBootstrap(
                RefreshProfilesCoreAsync,
                blockerReason: "ProfileSelectionRefresh",
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.profiles.refresh",
                    "Loading profiles..."),
                fallbackFailureMessage: LocalizationServiceLocator.GetText(
                    "profiles.load.error",
                    "Could not load profiles. Please try again."),
                failurePopupTitle: LocalizationServiceLocator.GetText(
                    "profiles.load.popup_title",
                    "Could not load profiles"),
                retryPopupTitle: LocalizationServiceLocator.GetText(
                    "profiles.retry.popup_title",
                    "Connection problem"));
        }

        private async Task PrepareSelectedProfileAndExitAsync()
        {
            if (isPreparingSelectedProfile_)
            {
                return;
            }

            isPreparingSelectedProfile_ = true;
            isLeavingScene_ = true;
            DisposeSelectionDelayBlockScope();

            try
            {
                IDisposable loadingScope = ScreenBlocker?.BlockScope(new ScreenBlockerRequest
                {
                    Reason = "ProfileSelectionLoad",
                    ShowLoadingWithTime = true,
                    LoadingMessage = LocalizationServiceLocator.GetText(
                        "loading.profiles.select",
                        "Loading profile..."),
                    ShowLoadingImmediately = true
                });
                try
                {
                    if (childGameStateSyncService_ != null)
                    {
                        bool loadedCurrentProfile = await childGameStateSyncService_.ReloadCurrentProfileAsync();
                        if (!isActiveAndEnabled)
                        {
                            isLeavingScene_ = false;
                            return;
                        }

                        if (!loadedCurrentProfile)
                        {
                            isLeavingScene_ = false;
                            ShowBlockingRetryPopup(
                                LocalizationServiceLocator.GetText(
                                    "profiles.selection.load_error",
                                    "Could not load profile. Please try again."),
                                () => _ = PrepareSelectedProfileAndExitAsync(),
                                LocalizationServiceLocator.GetText(
                                    "profiles.retry.popup_title",
                                    "Connection problem"));
                            return;
                        }
                    }
                }
                finally
                {
                    loadingScope?.Dispose();
                }

                if (!isActiveAndEnabled || delayedExitCoroutine_ != null)
                {
                    return;
                }

                selectionDelayBlockScope_ = ScreenBlocker?.BlockScope("ProfileSelectionDelay");
                delayedExitCoroutine_ = StartCoroutine(GoBackAfterDelay());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                DisposeSelectionDelayBlockScope();
                isLeavingScene_ = false;
            }
            finally
            {
                isPreparingSelectedProfile_ = false;
            }
        }

        protected override void HandleBlockingPopupClosed()
        {
            base.HandleBlockingPopupClosed();

            if (NavigationService == null)
            {
                return;
            }

            if (NavigationService.CanGoBack)
            {
                NavigationService.Back();
                return;
            }

            NavigationService.NavigateAsRoot(SceneType.AuthScene);
        }

        private void RefreshBackButtonVisibility()
        {
            if (backButtonRoot_ == null || NavigationService == null)
            {
                return;
            }

            backButtonRoot_.SetActive(NavigationService.CanGoBack);
        }

        private void ClearProfileButtons()
        {
            for (int index = 0; index < spawnedButtons_.Count; index++)
            {
                ProfileSelectionButtonView buttonView = spawnedButtons_[index];
                if (buttonView != null)
                {
                    Destroy(buttonView.gameObject);
                }
            }

            spawnedButtons_.Clear();
        }
        private IEnumerator GoBackAfterDelay()
        {
            yield return new WaitForSeconds(selectionDelaySeconds_);
            delayedExitCoroutine_ = null;
            pendingProfileIndex_ = -1;
            DisposeSelectionDelayBlockScope();
            UnsubscribeFromDispatcher();
            GoBack();
        }

        private void DisposeSelectionDelayBlockScope()
        {
            selectionDelayBlockScope_?.Dispose();
            selectionDelayBlockScope_ = null;
        }

        private async Task RefreshProfilesCoreAsync()
        {
            if (profilesService_ == null)
            {
                return;
            }

            await profilesService_.RefreshAsync();
            if (!isActiveAndEnabled)
            {
                return;
            }

            hasLoadedRemoteProfiles_ = true;
            RefreshBackButtonVisibility();
            RefreshProfileButtons();
        }

        private void ValidateSerializedReferences()
        {
            if (buttonListContainer_ == null ||
                profileButtonTemplate_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ProfileSelectionScene)} is missing one or more required serialized references.");
            }
        }
    }
}
