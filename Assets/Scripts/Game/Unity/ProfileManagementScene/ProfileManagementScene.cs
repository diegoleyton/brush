using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.Unity.UI;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Scenes;
using Game.Unity.UI;

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

        private ClientGameStateStore gameStateStore_;
        private IProfilesService profilesService_;
        private DataRepository repository_;
        private EventDispatcher dispatcher_;
        private ScreenBlocker screenBlocker_;
        private FeedbackMessage feedbackMessage_;
        private bool dispatcherSubscribed_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            ClientGameStateStore gameStateStore,
            IProfilesService profilesService,
            DataRepository repository,
            ScreenBlocker screenBlocker)
        {
            base.Construct(dispatcher, navigationService);
            dispatcher_ = dispatcher;
            gameStateStore_ = gameStateStore;
            profilesService_ = profilesService;
            repository_ = repository;
            screenBlocker_ = screenBlocker;
        }

        private void Awake()
        {
            sceneType_ = SceneType.ProfileManagementScene;
            ValidateSerializedReferences();
            feedbackMessage_ = GetComponent<FeedbackMessage>() ?? gameObject.AddComponent<FeedbackMessage>();
            feedbackMessage_.Configure(feedbackText_);
        }

        private void OnEnable()
        {
            if (initialized_)
            {
                RefreshProfileRows();
                _ = RefreshProfilesAsync();
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
            _ = RefreshProfilesAsync();
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

        public async void CreateProfile()
        {
            if (profilesService_ == null)
            {
                return;
            }

            bool creatingFirstProfile = gameStateStore_ == null || gameStateStore_.AllProfiles.Count == 0;

            string profileName = profileNameInput_ != null ? profileNameInput_.text.Trim() : string.Empty;
            string petName = petNameInput_ != null ? petNameInput_.text.Trim() : string.Empty;

            if (!profilesService_.CanUseName(profileName))
            {
                SetFeedback(LocalizationServiceLocator.GetText(
                    "profiles.name.invalid_or_used",
                    "Profile name is invalid or already in use."));
                return;
            }

            if (!profilesService_.CanUsePetName(petName))
            {
                SetFeedback(LocalizationServiceLocator.GetText("profiles.pet_name.invalid", "Pet name cannot be empty."));
                return;
            }

            Profile createdProfile;
            IDisposable blockScope = screenBlocker_?.BlockScope(
                "CreateProfile",
                showLoadingWithTime: true,
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.profiles.create",
                    "Creating profile..."));
            try
            {
                createdProfile = await profilesService_.CreateAsync(profileName, petName, pictureId: 1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SetFeedback(RemoteOperationMessageFormatter.Format(
                    exception,
                    LocalizationServiceLocator.GetText("profiles.create.error", "Could not create profile.")));
                return;
            }
            finally
            {
                blockScope?.Dispose();
            }

            if (createdProfile == null)
            {
                SetFeedback(LocalizationServiceLocator.GetText("profiles.create.error", "Could not create profile."));
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

            ShowSuccess(LocalizationServiceLocator.GetText("profiles.create.success", "Profile created."));

            if (creatingFirstProfile)
            {
                NavigationService.NavigateAsRoot(SceneType.RoomScene);
            }
        }

        private void RequestDeleteProfile(int profileIndex)
        {
            if (gameStateStore_ == null ||
                profileIndex < 0 ||
                profileIndex >= gameStateStore_.AllProfiles.Count)
            {
                return;
            }

            NavigationService.Navigate(
                SceneType.ConfirmPopup,
                new ConfirmPopupParams(
                    onAccept: () => DeleteProfileConfirmed(profileIndex),
                    onCancel: null));
        }

        private async void DeleteProfileConfirmed(int profileIndex)
        {
            if (profilesService_ == null)
            {
                return;
            }

            bool deleted;
            IDisposable blockScope = screenBlocker_?.BlockScope(
                "DeleteProfile",
                showLoadingWithTime: true,
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.profiles.delete",
                    "Deleting profile..."));
            try
            {
                deleted = await profilesService_.DeleteAsync(profileIndex);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SetFeedback(RemoteOperationMessageFormatter.Format(
                    exception,
                    LocalizationServiceLocator.GetText("profiles.delete.error", "Could not delete profile.")));
                return;
            }
            finally
            {
                blockScope?.Dispose();
            }

            if (!deleted)
            {
                SetFeedback(LocalizationServiceLocator.GetText("profiles.delete.minimum_one", "At least one profile must remain."));
                return;
            }

            ShowSuccess(LocalizationServiceLocator.GetText("profiles.delete.success", "Profile deleted."));
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
            if (profileListContainer_ == null || gameStateStore_ == null)
            {
                return;
            }

            ClearRows();

            int currentProfileIndex = gameStateStore_.CurrentProfileIndex;
            bool canDeleteAnyProfile = gameStateStore_.AllProfiles.Count > 1;

            for (int index = 0; index < gameStateStore_.AllProfiles.Count; index++)
            {
                Profile profile = gameStateStore_.AllProfiles[index];
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
            feedbackMessage_?.ShowError(message);
        }

        private void ShowSuccess(string message)
        {
            feedbackMessage_?.ShowSuccess(message, durationSeconds: 2f);
        }

        private void ClearFeedback()
        {
            SetFeedback(string.Empty);
        }

        private async Task RefreshProfilesAsync()
        {
            if (profilesService_ == null)
            {
                return;
            }

            try
            {
                IDisposable blockScope = screenBlocker_?.BlockScope(
                    "RefreshProfiles",
                    showLoadingWithTime: true,
                    loadingMessage: "Loading profiles...");
                try
                {
                    await profilesService_.RefreshAsync();
                }
                finally
                {
                    blockScope?.Dispose();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SetFeedback(RemoteOperationMessageFormatter.Format(
                    exception,
                    LocalizationServiceLocator.GetText("profiles.refresh.error", "Could not refresh profiles.")));
            }
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
