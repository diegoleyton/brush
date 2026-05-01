using System;
using System.Collections.Generic;
using System.Collections;

using Flowbit.Utilities.Core.Events;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Scenes;

using Flowbit.Utilities.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.ProfileSelectionScene
{
    /// <summary>
    /// Presents the available save profiles and lets the player switch between them.
    /// </summary>
    public sealed class ProfileSelectionScene : SceneBase
    {
        [SerializeField]
        private RectTransform buttonListContainer_;

        [SerializeField]
        private ProfileSelectionButtonView profileButtonTemplate_;

        [SerializeField]
        private float selectionDelaySeconds_ = 0.2f;

        private readonly List<ProfileSelectionButtonView> spawnedButtons_ = new();

        private DataRepository repository_;
        private EventDispatcher dispatcher_;
        private ScreenBlocker screenBlocker_;
        private bool dispatcherSubscribed_;
        private bool isLeavingScene_;
        private int pendingProfileIndex_ = -1;
        private Coroutine delayedExitCoroutine_;
        private IDisposable selectionDelayBlockScope_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            DataRepository repository,
            ScreenBlocker screenBlocker)
        {
            base.Construct(dispatcher, navigationService);
            dispatcher_ = dispatcher;
            repository_ = repository;
            screenBlocker_ = screenBlocker;
        }

        private void Awake()
        {
            sceneType_ = SceneType.ProfileSelectionScene;
            ValidateSerializedReferences();
        }

        private void OnEnable()
        {
            isLeavingScene_ = false;
            pendingProfileIndex_ = -1;

            if (initialized_)
            {
                RefreshProfileButtons();
            }
        }

        private void OnDestroy()
        {
            if (delayedExitCoroutine_ != null)
            {
                StopCoroutine(delayedExitCoroutine_);
                delayedExitCoroutine_ = null;
            }

            DisposeSelectionDelayBlockScope();
            UnsubscribeFromDispatcher();
        }

        protected override void Initialize()
        {
            SubscribeToDispatcher();
            RefreshProfileButtons();
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

        private void OnProfileSwitched(ProfileSwitchedEvent _)
        {
            RefreshProfileButtons();

            if (pendingProfileIndex_ >= 0 && delayedExitCoroutine_ == null)
            {
                isLeavingScene_ = true;
                DisposeSelectionDelayBlockScope();
                selectionDelayBlockScope_ = screenBlocker_?.BlockScope("ProfileSelectionDelay");
                delayedExitCoroutine_ = StartCoroutine(GoBackAfterDelay());
            }
        }

        private void OnProfileUpdated(ProfileUpdatedEvent _)
        {
            if (isLeavingScene_)
            {
                return;
            }

            RefreshProfileButtons();
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
            if (buttonListContainer_ == null || repository_ == null)
            {
                return;
            }

            ClearProfileButtons();

            int currentProfileIndex = repository_.CurrentProfile != null
                ? repository_.AllProfiles.IndexOf(repository_.CurrentProfile)
                : -1;

            for (int index = 0; index < repository_.AllProfiles.Count; index++)
            {
                Profile profile = repository_.AllProfiles[index];
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
            if (repository_ == null ||
                profileIndex < 0 ||
                profileIndex >= repository_.AllProfiles.Count)
            {
                return;
            }

            int currentProfileIndex = repository_.CurrentProfile != null
                ? repository_.AllProfiles.IndexOf(repository_.CurrentProfile)
                : -1;
            if (currentProfileIndex == profileIndex)
            {
                GoBack();
                return;
            }

            pendingProfileIndex_ = profileIndex;
            repository_.SwitchProfile(profileIndex);
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
