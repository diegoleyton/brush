using System;
using System.Collections.Generic;
using System.Linq;

using Flowbit.Utilities.Core.Events;

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

        private bool dispatcherSubscribed_;
        private bool initialized_;
        private bool inventoryButtonsBound_;

        [SerializeField]
        [FormerlySerializedAs("paletteList_")]
        private RoomInventoryListUI inventoryList_;

        [SerializeField]
        private InteractionPointType selectedInventoryType_ = InteractionPointType.PLACEABLE_OBJECT;

        [SerializeField]
        private RoomInventoryCategoryButtonBinding[] inventoryCategoryButtons_ =
            Array.Empty<RoomInventoryCategoryButtonBinding>();

        [Inject]
        public void Construct(
            DataRepository repository,
            EventDispatcher dispatcher,
            RoomInventorySelectionState selectionState)
        {
            repository_ = repository;
            dispatcher_ = dispatcher;
            selectionState_ = selectionState;
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
            dispatcher_.Subscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
            dispatcher_.Subscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcherSubscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!dispatcherSubscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomInventoryDropAcceptedEvent>(OnRoomInventoryDropAccepted);
            dispatcher_.Unsubscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
            dispatcher_.Unsubscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcherSubscribed_ = false;
        }

        private void OnInventoryUpdated(InventoryUpdatedEvent _)
        {
            RefreshInventoryList();
        }

        private void OnProfileSwitched(ProfileSwitchedEvent _)
        {
            RefreshInventoryList();
        }

        private void SelectInventoryCategory(InteractionPointType interactionPointType)
        {
            selectedInventoryType_ = interactionPointType;
            selectionState_?.SetCurrentInteractionPointType(interactionPointType);
            RefreshInventoryList();
            RefreshDropAreas();
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
            dispatcher_?.Send(new ShowInteractionPointsEvent(selectedInventoryType_));
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
                        (InteractionPointType.FACE, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(new RoomFaceAppliedEvent(eventData.Data.ItemId))
                    },
                    {
                        (InteractionPointType.SKIN, RoomTargetKind.ROOM),
                        eventData => dispatcher_.Send(new RoomSkinAppliedEvent(eventData.Data.ItemId))
                    }
                };
        }

    }
}
