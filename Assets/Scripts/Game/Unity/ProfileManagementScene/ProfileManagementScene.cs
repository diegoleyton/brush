using System;
using System.Collections.Generic;

using Flowbit.Utilities.Core.Events;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Scenes;

using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.ProfileManagementScene
{
    /// <summary>
    /// Lets the player create and delete profiles using fully serialized scene references.
    /// </summary>
    public sealed class ProfileManagementScene : SceneBase
    {
        [SerializeField]
        private RectTransform profileListContainer_;

        [SerializeField]
        private ProfileManagementProfileRowView profileRowTemplate_;

        [SerializeField]
        private InputField profileNameInput_;

        [SerializeField]
        private InputField petNameInput_;

        [SerializeField]
        private Text feedbackText_;

        [SerializeField]
        private Text brushTimeText_;

        [SerializeField]
        private Slider brushTimeSlider_;

        private readonly List<ProfileManagementProfileRowView> spawnedRows_ = new();

        private DataRepository repository_;
        private EventDispatcher dispatcher_;
        private bool dispatcherSubscribed_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            DataRepository repository)
        {
            base.Construct(dispatcher, navigationService);
            dispatcher_ = dispatcher;
            repository_ = repository;
        }

        private void Awake()
        {
            sceneType_ = SceneType.ProfileManagementScene;
            ValidateSerializedReferences();
        }

        private void OnEnable()
        {
            if (initialized_)
            {
                RefreshProfileRows();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        protected override void Initialize()
        {
            SubscribeToDispatcher();
            ConfigureBrushTimeSlider();
            ClearFeedback();
            RefreshProfileRows();
            RefreshBrushTime();
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

        public void CreateProfile()
        {
            if (repository_ == null)
            {
                return;
            }

            string profileName = profileNameInput_ != null ? profileNameInput_.text.Trim() : string.Empty;
            string petName = petNameInput_ != null ? petNameInput_.text.Trim() : string.Empty;

            if (!repository_.CanUseName(profileName))
            {
                SetFeedback("Profile name is invalid or already in use.");
                return;
            }

            if (!repository_.CanUsePetName(petName))
            {
                SetFeedback("Pet name cannot be empty.");
                return;
            }

            Profile createdProfile = repository_.CreateProfile(profileName, petName, pictureId: 1);
            if (createdProfile == null)
            {
                SetFeedback("Could not create profile.");
                return;
            }

            if (profileNameInput_ != null)
            {
                profileNameInput_.text = string.Empty;
            }

            if (petNameInput_ != null)
            {
                petNameInput_.text = string.Empty;
            }

            SetFeedback("Profile created.");
        }

        private void RequestDeleteProfile(int profileIndex)
        {
            if (repository_ == null ||
                profileIndex < 0 ||
                profileIndex >= repository_.AllProfiles.Count)
            {
                return;
            }

            NavigationService.Navigate(
                SceneType.ConfirmPopup,
                new ConfirmPopupParams(
                    onAccept: () => DeleteProfileConfirmed(profileIndex),
                    onCancel: null));
        }

        private void DeleteProfileConfirmed(int profileIndex)
        {
            if (repository_ == null)
            {
                return;
            }

            if (!repository_.DeleteProfile(profileIndex))
            {
                SetFeedback("At least one profile must remain.");
                return;
            }

            SetFeedback("Profile deleted.");
        }

        private void OnProfileSwitched(ProfileSwitchedEvent _)
        {
            RefreshProfileRows();
            RefreshBrushTime();
        }

        private void OnProfileUpdated(ProfileUpdatedEvent _)
        {
            RefreshProfileRows();
            RefreshBrushTime();
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

        private void RefreshProfileRows()
        {
            if (profileListContainer_ == null || repository_ == null)
            {
                return;
            }

            ClearRows();

            int currentProfileIndex = repository_.CurrentProfile != null
                ? repository_.AllProfiles.IndexOf(repository_.CurrentProfile)
                : -1;
            bool canDeleteAnyProfile = repository_.AllProfiles.Count > 1;

            for (int index = 0; index < repository_.AllProfiles.Count; index++)
            {
                Profile profile = repository_.AllProfiles[index];
                if (profile == null)
                {
                    continue;
                }

                int profileIndex = index;
                ProfileManagementProfileRowView rowView =
                    Instantiate(profileRowTemplate_, profileListContainer_, false);
                rowView.gameObject.name = $"ProfileRow_{profileIndex + 1}";
                rowView.gameObject.SetActive(true);
                rowView.Configure(
                    profile.Name,
                    isCurrent: profileIndex == currentProfileIndex,
                    canDelete: canDeleteAnyProfile,
                    deleteAction: () => RequestDeleteProfile(profileIndex));
                spawnedRows_.Add(rowView);
            }
        }

        private void ConfigureBrushTimeSlider()
        {
            if (brushTimeSlider_ == null)
            {
                return;
            }

            if (Application.isEditor)
            {
                brushTimeSlider_.minValue = 0.1f;
            }

            brushTimeSlider_.onValueChanged.RemoveListener(OnBrushTimeSliderChanged);
            brushTimeSlider_.onValueChanged.AddListener(OnBrushTimeSliderChanged);
        }

        private void RefreshBrushTime()
        {
            if (brushTimeSlider_ == null || repository_ == null)
            {
                return;
            }

            float brushTimeMinutes = repository_.GetBrushSessionDurationMinutes();
            brushTimeSlider_.SetValueWithoutNotify(brushTimeMinutes);
            UpdateBrushTimeText(brushTimeMinutes);
        }

        private void OnBrushTimeSliderChanged(float value)
        {
            UpdateBrushTimeText(value);
            repository_?.SetBrushSessionDurationMinutes(value);
        }

        private void UpdateBrushTimeText(float value)
        {
            if (brushTimeText_ == null)
            {
                return;
            }

            brushTimeText_.text = $"{value:0.0}";
        }

        private void ClearRows()
        {
            for (int index = 0; index < spawnedRows_.Count; index++)
            {
                ProfileManagementProfileRowView rowView = spawnedRows_[index];
                if (rowView != null)
                {
                    Destroy(rowView.gameObject);
                }
            }

            spawnedRows_.Clear();
        }

        private void SetFeedback(string message)
        {
            if (feedbackText_ != null)
            {
                feedbackText_.text = message;
            }
        }

        private void ClearFeedback()
        {
            SetFeedback(string.Empty);
        }

        private void ValidateSerializedReferences()
        {
            if (profileListContainer_ == null ||
                profileRowTemplate_ == null ||
                profileNameInput_ == null ||
                petNameInput_ == null ||
                feedbackText_ == null ||
                brushTimeText_ == null ||
                brushTimeSlider_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ProfileManagementScene)} is missing one or more required serialized references.");
            }
        }
    }
}
