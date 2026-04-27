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

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Game.Unity.RoomScene
{
    [Serializable]
    public sealed class RoomInventoryCategoryButtonBinding
    {
        public InteractionPointType InteractionPointType = InteractionPointType.PLACEABLE_OBJECT;
        public Button Button;

        [NonSerialized]
        public UnityAction ClickAction;
    }

    /// <summary>
    /// Orchestrates the room scene UI and translates visual interactions into room events.
    /// </summary>
    public sealed class RoomController : MonoBehaviour
    {
        private Dictionary<(InteractionPointType, RoomTargetKind), Action<RoomInventoryDropAcceptedEvent>>
            dropAcceptedHandlers_;

        private DataRepository repository_;
        private EventDispatcher dispatcher_;
        private RoomInventorySelectionState selectionState_;
        private IGameLogger logger_;

        private bool dispatcherSubscribed_;
        private bool initialized_;
        private bool inventoryButtonsBound_;
        private bool restoreInventoryAfterDrag_;

        [SerializeField]
        [FormerlySerializedAs("paletteList_")]
        private RoomInventoryListUI inventoryList_;

        [SerializeField]
        private RoomInventoryView inventoryView_;

        [SerializeField]
        private InteractionPointType selectedInventoryType_ = InteractionPointType.PLACEABLE_OBJECT;

        [SerializeField]
        private RoomInventoryCategoryButtonBinding[] inventoryCategoryButtons_ =
            Array.Empty<RoomInventoryCategoryButtonBinding>();

        [Inject]
        public void Construct(
            DataRepository repository,
            EventDispatcher dispatcher,
            RoomInventorySelectionState selectionState,
            IGameLogger logger)
        {
            repository_ = repository;
            dispatcher_ = dispatcher;
            selectionState_ = selectionState;
            logger_ = logger;
        }

        private void Awake()
        {
            if (selectionState_ != null &&
                selectionState_.CurrentInteractionPointType != selectedInventoryType_)
            {
                selectionState_.SetCurrentInteractionPointType(selectedInventoryType_);
            }

            SubscribeToDispatcher();
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
            initialized_ = true;
        }

        private void ValidateSceneReferences()
        {
            if (inventoryList_ == null)
            {
                inventoryList_ = GetComponentInChildren<RoomInventoryListUI>(true);
            }

            if (inventoryList_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomController)} requires a {nameof(RoomInventoryListUI)} reference.");
            }

            if (inventoryView_ == null)
            {
                inventoryView_ = GetComponentInChildren<RoomInventoryView>(true);
            }
        }

        private void BindInventoryCategoryButtons()
        {
            if (inventoryButtonsBound_ || inventoryCategoryButtons_ == null)
            {
                return;
            }

            for (int index = 0; index < inventoryCategoryButtons_.Length; index++)
            {
                RoomInventoryCategoryButtonBinding binding = inventoryCategoryButtons_[index];
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
            if (!inventoryButtonsBound_ || inventoryCategoryButtons_ == null)
            {
                return;
            }

            for (int index = 0; index < inventoryCategoryButtons_.Length; index++)
            {
                RoomInventoryCategoryButtonBinding binding = inventoryCategoryButtons_[index];
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
            dispatcher_.Subscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
            dispatcher_.Subscribe<ProfileSwitchedEvent>(OnProfileSwitched);
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
            dispatcher_.Unsubscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
            dispatcher_.Unsubscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_.Unsubscribe<SwitchListEvent>(OnSwitchList);
            dispatcherSubscribed_ = false;
        }

        private void OnRoomInventoryDragStarted(RoomInventoryDragStartedEvent _)
        {
            restoreInventoryAfterDrag_ = inventoryView_ != null && !inventoryView_.IsHidden;
            inventoryView_?.Hide();
        }

        private void OnRoomInventoryDragEnded(RoomInventoryDragEndedEvent eventData)
        {
            if (eventData?.Data?.InteractionPointType == InteractionPointType.FOOD && eventData.DropAccepted)
            {
                return;
            }

            RestoreInventoryAfterDragIfNeeded();
        }

        private void OnPetFoodAnimationCompleted(PetFoodAnimationCompletedEvent _)
        {
            RestoreInventoryAfterDragIfNeeded();
        }

        private void RestoreInventoryAfterDragIfNeeded()
        {
            if (!restoreInventoryAfterDrag_)
            {
                return;
            }

            restoreInventoryAfterDrag_ = false;
            inventoryView_?.Show();
        }

        private void OnInventoryUpdated(InventoryUpdatedEvent _)
        {
            RefreshInventoryList();
        }

        private void OnProfileSwitched(ProfileSwitchedEvent _)
        {
            RefreshInventoryList();
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
            if (inventoryCategoryButtons_ == null)
            {
                return;
            }

            for (int index = 0; index < inventoryCategoryButtons_.Length; index++)
            {
                RoomInventoryCategoryButtonBinding binding = inventoryCategoryButtons_[index];
                if (binding?.Button == null)
                {
                    continue;
                }

                binding.Button.interactable = binding.InteractionPointType != selectedInventoryType_;
            }
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
                .Where(entry => entry.Value != 0)
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
                $"[RoomUI] Drop accepted. Type: {eventData.Data.InteractionPointType}, Item: {eventData.Data.ItemId}, TargetKind: {eventData.DropArea.RoomTargetKind}, Target: {eventData.DropArea.TargetId}, Parent: {eventData.DropArea.ParentTargetId}");

            if (dropAcceptedHandlers_ == null)
            {
                BuildDropAcceptedHandlers();
            }

            if (dropAcceptedHandlers_.TryGetValue(
                    (eventData.Data.InteractionPointType, eventData.DropArea.RoomTargetKind),
                    out Action<RoomInventoryDropAcceptedEvent> handler))
            {
                handler.Invoke(eventData);
            }
        }

        private void BuildDropAcceptedHandlers()
        {
            dropAcceptedHandlers_ =
                new Dictionary<(InteractionPointType, RoomTargetKind), Action<RoomInventoryDropAcceptedEvent>>
                {
                    {
                        (InteractionPointType.PLACEABLE_OBJECT, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(
                            new RoomObjectPlacedEvent(eventData.Data.ItemId, eventData.DropArea.TargetId))
                    },
                    {
                        (InteractionPointType.PLACEABLE_OBJECT, RoomTargetKind.PLACEABLE_OBJECT),
                        eventData => dispatcher_.Send(new RoomChildObjectPlacedEvent(
                            eventData.Data.ItemId,
                            eventData.DropArea.ParentTargetId,
                            eventData.DropArea.TargetId))
                    },
                    {
                        (InteractionPointType.PAINT, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(
                            new RoomPaintAppliedEvent(eventData.Data.ItemId, eventData.DropArea.TargetId))
                    },
                    {
                        (InteractionPointType.PAINT, RoomTargetKind.PLACEABLE_OBJECT),
                        eventData =>
                        {
                            if (eventData.DropArea.ParentTargetId >= 0)
                            {
                                dispatcher_.Send(new RoomChildObjectPaintAppliedEvent(
                                    eventData.Data.ItemId,
                                    eventData.DropArea.ParentTargetId,
                                    eventData.DropArea.TargetId));
                                return;
                            }

                            dispatcher_.Send(new RoomObjectPaintAppliedEvent(
                                eventData.Data.ItemId,
                                eventData.DropArea.TargetId));
                        }
                    },
                    {
                        (InteractionPointType.EYES, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(new RoomEyesAppliedEvent(eventData.Data.ItemId))
                    },
                    {
                        (InteractionPointType.HAT, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(new RoomHatAppliedEvent(eventData.Data.ItemId))
                    },
                    {
                        (InteractionPointType.SKIN, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(new RoomSkinAppliedEvent(eventData.Data.ItemId))
                    },
                    {
                        (InteractionPointType.DRESS, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(new RoomDressAppliedEvent(eventData.Data.ItemId))
                    },
                    {
                        (InteractionPointType.FOOD, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(new RoomFoodAppliedEvent(eventData.Data.ItemId))
                    }
                };
        }

    }
}
