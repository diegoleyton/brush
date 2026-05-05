using System;
using System.Collections.Generic;
using System.Linq;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Definitions.Events;
using Game.Unity.Scenes;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Orchestrates the room scene UI and translates visual interactions into room events.
    /// </summary>
    public sealed class RoomController : MonoBehaviour
    {
        private Dictionary<InteractionPointType, Action<RoomInventoryDropAcceptedEvent>> dropAcceptedHandlers_;

        private DataRepository repository_;
        private EventDispatcher dispatcher_;
        private RoomInventorySelectionState selectionState_;
        private IGameLogger logger_;
        private IGameNavigationService navigationService_;

        private bool dispatcherSubscribed_;
        private bool initialized_;
        private bool inventoryButtonsBound_;
        private bool restoreInventoryAfterDrag_;
        private bool waitForPaintEffectAfterDrag_;
        private bool waitForSkinEffectAfterDrag_;
        [SerializeField]
        [FormerlySerializedAs("paletteList_")]
        private RoomInventoryListUI inventoryList_;

        [SerializeField]
        private RoomInventoryView inventoryView_;

        [SerializeField]
        private InputField petNameInput_;

        [SerializeField]
        private RewardButton rewardButton_;

        [SerializeField]
        private InteractionPointType selectedInventoryType_ = InteractionPointType.PLACEABLE_OBJECT;

        private bool suppressPetNameInputCallback_;

        [Inject]
        public void Construct(
            DataRepository repository,
            EventDispatcher dispatcher,
            RoomInventorySelectionState selectionState,
            IGameLogger logger,
            IGameNavigationService navigationService)
        {
            repository_ = repository;
            dispatcher_ = dispatcher;
            selectionState_ = selectionState;
            logger_ = logger;
            navigationService_ = navigationService;
        }

        private void Awake()
        {
            ValidateSerializedReferences();

            if (selectionState_ != null &&
                selectionState_.CurrentInteractionPointType != selectedInventoryType_)
            {
                selectionState_.SetCurrentInteractionPointType(selectedInventoryType_);
            }

            SubscribeToDispatcher();
            BindPetNameInput();
            RefreshPetNameInput();
            BindRewardButton();
            RefreshRewardButton();
            BindInventoryCategoryButtons();
            BuildDropAcceptedHandlers();
        }

        private void OnEnable()
        {
            if (initialized_)
            {
                RefreshDropAreas();
            }
        }

        private void OnDestroy()
        {
            UnbindPetNameInput();
            UnbindInventoryCategoryButtons();
            UnsubscribeFromDispatcher();
        }

        /// <summary>
        /// Initializes the room UI for the active scene.
        /// </summary>
        public void Initialize()
        {
            if (initialized_)
            {
                return;
            }

            ValidateSceneReferences();
            RefreshInventoryList();
            RefreshDropAreas();
            RefreshPetNameInput();
            RefreshRewardButton();
            initialized_ = true;
        }

        public void GoToBrushScene()
        {
            if (navigationService_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires a {nameof(IGameNavigationService)} dependency.");
            }

            navigationService_.Navigate(SceneType.ConfirmPopup, new ConfirmPopupParams(() =>
            {
                navigationService_.Navigate(SceneType.BrushScene);
            }, null));
        }

        public void GoToProfileSelectionScene()
        {
            if (navigationService_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires a {nameof(IGameNavigationService)} dependency.");
            }

            navigationService_.Navigate(SceneType.ProfileSelectionScene);
        }

        public void GoToMarketScene()
        {
            if (navigationService_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires a {nameof(IGameNavigationService)} dependency.");
            }

            navigationService_.Navigate(SceneType.MarketScene);
        }

        public void GoToRewardsScene()
        {
            if (navigationService_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires a {nameof(IGameNavigationService)} dependency.");
            }

            navigationService_.Navigate(SceneType.RewardsScene);
        }

        private void ValidateSceneReferences()
        {
            ValidateSerializedReferences();
        }

        private void BindInventoryCategoryButtons()
        {
            if (inventoryButtonsBound_ || inventoryView_ == null)
            {
                return;
            }

            IReadOnlyList<RoomInventoryCategoryButtonBinding> inventoryCategoryButtons =
                inventoryView_.CategoryButtons;

            if (inventoryCategoryButtons == null)
            {
                return;
            }

            for (int index = 0; index < inventoryCategoryButtons.Count; index++)
            {
                RoomInventoryCategoryButtonBinding binding = inventoryCategoryButtons[index];
                if (binding?.Button == null)
                {
                    continue;
                }

                InteractionPointType interactionPointType = binding.InteractionPointType;
                binding.ClickAction = () => SelectInventoryCategory(interactionPointType);
                binding.Button.onClick.AddListener(binding.ClickAction);
            }

            inventoryButtonsBound_ = true;
        }

        private void UnbindInventoryCategoryButtons()
        {
            if (!inventoryButtonsBound_ || inventoryView_ == null)
            {
                return;
            }

            IReadOnlyList<RoomInventoryCategoryButtonBinding> inventoryCategoryButtons =
                inventoryView_.CategoryButtons;

            if (inventoryCategoryButtons == null)
            {
                inventoryButtonsBound_ = false;
                return;
            }

            for (int index = 0; index < inventoryCategoryButtons.Count; index++)
            {
                RoomInventoryCategoryButtonBinding binding = inventoryCategoryButtons[index];
                if (binding?.Button != null && binding.ClickAction != null)
                {
                    binding.Button.onClick.RemoveListener(binding.ClickAction);
                    binding.ClickAction = null;
                }
            }

            inventoryButtonsBound_ = false;
        }

        private void SubscribeToDispatcher()
        {
            if (dispatcherSubscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<RoomInventoryDropAcceptedEvent>(OnRoomInventoryDropAccepted);
            dispatcher_.Subscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Subscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            dispatcher_.Subscribe<PetFoodAnimationCompletedEvent>(OnPetFoodAnimationCompleted);
            dispatcher_.Subscribe<RoomPaintEffectCompletedEvent>(OnRoomPaintEffectCompleted);
            dispatcher_.Subscribe<PetSkinEffectCompletedEvent>(OnPetSkinEffectCompleted);
            dispatcher_.Subscribe<RoomDataItemApplyFailedEvent>(OnRoomDataItemApplyFailed);
            dispatcher_.Subscribe<PetDataApplyFailedEvent>(OnPetDataApplyFailed);
            dispatcher_.Subscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
            dispatcher_.Subscribe<PendingRewardEvent>(OnPendingRewardChanged);
            dispatcher_.Subscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_.Subscribe<ProfileUpdatedEvent>(OnProfileUpdated);
            dispatcher_.Subscribe<SwitchListEvent>(OnSwitchList);
            dispatcherSubscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!dispatcherSubscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomInventoryDropAcceptedEvent>(OnRoomInventoryDropAccepted);
            dispatcher_.Unsubscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Unsubscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            dispatcher_.Unsubscribe<PetFoodAnimationCompletedEvent>(OnPetFoodAnimationCompleted);
            dispatcher_.Unsubscribe<RoomPaintEffectCompletedEvent>(OnRoomPaintEffectCompleted);
            dispatcher_.Unsubscribe<RoomDataItemApplyFailedEvent>(OnRoomDataItemApplyFailed);
            dispatcher_.Unsubscribe<PetSkinEffectCompletedEvent>(OnPetSkinEffectCompleted);
            dispatcher_.Unsubscribe<PetDataApplyFailedEvent>(OnPetDataApplyFailed);
            dispatcher_.Unsubscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
            dispatcher_.Unsubscribe<PendingRewardEvent>(OnPendingRewardChanged);
            dispatcher_.Unsubscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_.Unsubscribe<ProfileUpdatedEvent>(OnProfileUpdated);
            dispatcher_.Unsubscribe<SwitchListEvent>(OnSwitchList);
            dispatcherSubscribed_ = false;
        }

        private void OnRoomInventoryDragStarted(RoomInventoryDragStartedEvent _)
        {
            restoreInventoryAfterDrag_ = inventoryView_ != null && !inventoryView_.IsHidden;
            waitForPaintEffectAfterDrag_ = false;
            waitForSkinEffectAfterDrag_ = false;
            inventoryView_?.Hide();
        }

        private void OnRoomInventoryDragEnded(RoomInventoryDragEndedEvent eventData)
        {
            if (eventData?.Data?.InteractionPointType == InteractionPointType.FOOD && eventData.DropAccepted)
            {
                return;
            }

            if (eventData?.Data?.InteractionPointType == InteractionPointType.PAINT &&
                eventData.DropAccepted &&
                waitForPaintEffectAfterDrag_)
            {
                return;
            }

            if (eventData?.Data?.InteractionPointType == InteractionPointType.SKIN &&
                eventData.DropAccepted &&
                waitForSkinEffectAfterDrag_)
            {
                return;
            }

            RestoreInventoryAfterDragIfNeeded();
        }

        private void OnPetFoodAnimationCompleted(PetFoodAnimationCompletedEvent _)
        {
            RestoreInventoryAfterDragIfNeeded();
        }

        private void OnRoomPaintEffectCompleted(RoomPaintEffectCompletedEvent _)
        {
            if (!waitForPaintEffectAfterDrag_)
            {
                return;
            }

            waitForPaintEffectAfterDrag_ = false;
            RestoreInventoryAfterDragIfNeeded();
        }

        private void OnPetSkinEffectCompleted(PetSkinEffectCompletedEvent _)
        {
            if (!waitForSkinEffectAfterDrag_)
            {
                return;
            }

            waitForSkinEffectAfterDrag_ = false;
            RestoreInventoryAfterDragIfNeeded();
        }

        private void OnRoomDataItemApplyFailed(RoomDataItemApplyFailedEvent eventData)
        {
            if (eventData == null ||
                eventData.ItemType != InteractionPointType.PAINT ||
                !waitForPaintEffectAfterDrag_)
            {
                return;
            }

            waitForPaintEffectAfterDrag_ = false;
            RestoreInventoryAfterDragIfNeeded();
        }

        private void OnPetDataApplyFailed(PetDataApplyFailedEvent eventData)
        {
            if (eventData == null ||
                eventData.ItemType != InteractionPointType.SKIN ||
                !waitForSkinEffectAfterDrag_)
            {
                return;
            }

            waitForSkinEffectAfterDrag_ = false;
            RestoreInventoryAfterDragIfNeeded();
        }

        private void RestoreInventoryAfterDragIfNeeded()
        {
            if (!restoreInventoryAfterDrag_)
            {
                return;
            }

            restoreInventoryAfterDrag_ = false;
            inventoryView_?.ShowWithDelay();
        }

        private void OnInventoryUpdated(InventoryUpdatedEvent _)
        {
            RefreshInventoryList();
        }

        private void OnProfileSwitched(ProfileSwitchedEvent _)
        {
            RefreshInventoryList();
            RefreshPetNameInput();
            RefreshRewardButton();
        }

        private void OnProfileUpdated(ProfileUpdatedEvent _)
        {
            RefreshPetNameInput();
        }

        private void OnPendingRewardChanged(PendingRewardEvent _)
        {
            RefreshRewardButton();
        }

        private void OnSwitchList(SwitchListEvent eventData)
        {
            if (eventData == null)
            {
                return;
            }

            selectedInventoryType_ = eventData.InteractionPointType;
            UpdateInventoryCategoryButtons();
        }

        private void SelectInventoryCategory(InteractionPointType interactionPointType)
        {
            selectedInventoryType_ = interactionPointType;
            selectionState_?.SetCurrentInteractionPointType(interactionPointType);
            RefreshInventoryList();
        }

        private void BindPetNameInput()
        {
            if (petNameInput_ == null)
            {
                return;
            }

            petNameInput_.onEndEdit.RemoveListener(OnPetNameInputEnded);
            petNameInput_.onEndEdit.AddListener(OnPetNameInputEnded);
        }

        private void UnbindPetNameInput()
        {
            if (petNameInput_ == null)
            {
                return;
            }

            petNameInput_.onEndEdit.RemoveListener(OnPetNameInputEnded);
        }

        private void RefreshPetNameInput()
        {
            if (petNameInput_ == null)
            {
                return;
            }

            string petName = repository_?.CurrentProfile?.PetData?.Name ?? string.Empty;
            if (petNameInput_.text == petName)
            {
                return;
            }

            suppressPetNameInputCallback_ = true;
            petNameInput_.text = petName;
            suppressPetNameInputCallback_ = false;
        }

        private void OnPetNameInputEnded(string inputValue)
        {
            if (suppressPetNameInputCallback_ || repository_?.CurrentProfile?.PetData == null)
            {
                return;
            }

            string normalizedPetName = (inputValue ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalizedPetName))
            {
                RefreshPetNameInput();
                return;
            }

            repository_.SetPetName(normalizedPetName);
            RefreshPetNameInput();
        }

        private void RefreshInventoryList()
        {
            if (inventoryList_ == null)
            {
                return;
            }

            inventoryList_.SetData(BuildInventoryItems(selectedInventoryType_));
            inventoryList_.ScrollToIndex(0);
        }

        private void RefreshDropAreas()
        {
            UpdateInventoryCategoryButtons();
        }

        private void UpdateInventoryCategoryButtons()
        {
            inventoryView_?.SetSelectedCategory(selectedInventoryType_);
        }

        private List<RoomInventoryItemData> BuildInventoryItems(InteractionPointType interactionPointType)
        {
            Dictionary<int, int> inventoryItems =
                repository_?.CurrentProfile?.InventoryData?.GetInventoryItems(interactionPointType);
            if (inventoryItems == null)
            {
                return new List<RoomInventoryItemData>();
            }

            return inventoryItems
                .Where(entry => entry.Value != 0 && Core.Configuration.ItemCatalog.IsValidItemId(interactionPointType, entry.Key))
                .OrderBy(entry => entry.Key)
                .Select(entry => new RoomInventoryItemData
                {
                    ItemId = entry.Key,
                    InteractionPointType = interactionPointType,
                    Name = $"{RoomItemVisuals.GetCategoryDisplayName(interactionPointType)} {entry.Key}",
                    Color = RoomItemVisuals.GetItemColor(interactionPointType, entry.Key),
                    Quantity = entry.Value
                })
                .ToList();
        }

        private void OnRoomInventoryDropAccepted(RoomInventoryDropAcceptedEvent eventData)
        {
            if (eventData?.DropArea == null || eventData.Data == null)
            {
                return;
            }

            if (eventData.Data.InteractionPointType != eventData.DropArea.SupportedInventoryType)
            {
                return;
            }

            if (dispatcher_ == null)
            {
                return;
            }

            logger_?.Log(
                $"[RoomUI] Drop accepted. Type: {eventData.Data.InteractionPointType}, Item: {eventData.Data.ItemId}, Target: {eventData.DropArea.TargetId}");

            if (eventData.Data.InteractionPointType == InteractionPointType.PAINT)
            {
                waitForPaintEffectAfterDrag_ = true;
            }

            if (eventData.Data.InteractionPointType == InteractionPointType.SKIN)
            {
                waitForSkinEffectAfterDrag_ = true;
            }

            if (dropAcceptedHandlers_ == null)
            {
                BuildDropAcceptedHandlers();
            }

            if (dropAcceptedHandlers_.TryGetValue(
                    eventData.Data.InteractionPointType,
                    out Action<RoomInventoryDropAcceptedEvent> handler))
            {
                handler.Invoke(eventData);
            }
        }

        private void BuildDropAcceptedHandlers()
        {
            dropAcceptedHandlers_ =
                new Dictionary<InteractionPointType, Action<RoomInventoryDropAcceptedEvent>>
                {
                    {
                        InteractionPointType.PLACEABLE_OBJECT,
                        eventData => dispatcher_.Send(
                            new RoomObjectPlacedEvent(eventData.Data.ItemId, eventData.DropArea.TargetId))
                    },
                    {
                        InteractionPointType.PAINT,
                        eventData =>
                        {
                            dispatcher_.Send(new RoomPaintDropPositionCapturedEvent(eventData.DropScreenPosition));
                            dispatcher_.Send(
                                new RoomPaintAppliedEvent(eventData.Data.ItemId, eventData.DropArea.TargetId));
                        }
                    },
                    {
                        InteractionPointType.EYES,
                        eventData => dispatcher_.Send(new RoomEyesAppliedEvent(eventData.Data.ItemId))
                    },
                    {
                        InteractionPointType.HAT,
                        eventData => dispatcher_.Send(new RoomHatAppliedEvent(eventData.Data.ItemId))
                    },
                    {
                        InteractionPointType.SKIN,
                        eventData =>
                        {
                            dispatcher_.Send(new PetSkinDropPositionCapturedEvent(eventData.DropScreenPosition));
                            dispatcher_.Send(new RoomSkinAppliedEvent(eventData.Data.ItemId));
                        }
                    },
                    {
                        InteractionPointType.DRESS,
                        eventData => dispatcher_.Send(new RoomDressAppliedEvent(eventData.Data.ItemId))
                    },
                    {
                        InteractionPointType.FOOD,
                        eventData =>
                        {
                            dispatcher_.Send(new PetFoodDropPositionCapturedEvent(eventData.DropScreenPosition));
                            dispatcher_.Send(new RoomFoodAppliedEvent(eventData.Data.ItemId));
                        }
                    }
                };
        }

        private void ValidateSerializedReferences()
        {
            if (inventoryView_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires a {nameof(RoomInventoryView)} reference.");
            }

            if (inventoryList_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires a {nameof(RoomInventoryListUI)} reference.");
            }

            if (petNameInput_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires an {nameof(InputField)} reference for the pet name input.");
            }

            if (rewardButton_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires a {nameof(RewardButton)} reference.");
            }
        }

        private void BindRewardButton()
        {
            rewardButton_?.Initialize(GoToRewardsScene);
        }

        private void RefreshRewardButton()
        {
            rewardButton_?.SetPendingRewardCount(repository_?.CurrentProfile?.PendingRewardCount ?? 0);
        }

    }
}
