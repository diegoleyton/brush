using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.DragAndDrop;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Unity.Definitions;
using Game.Unity.Definitions.Events;
using Game.Unity.Settings;

using UnityEngine;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Generic room drop area that reacts to the selected inventory category and reports
    /// a logical room target when a drop is accepted.
    /// </summary>
    public sealed class RoomDropArea : UIDropTarget
    {
        private EventDispatcher dispatcher_;
        private RoomSettings roomSettings_;

        [SerializeField]
        private InteractionPointType supportedInventoryType_ = InteractionPointType.PLACEABLE_OBJECT;

        [SerializeField]
        private RoomTargetKind roomTargetKind_ = RoomTargetKind.ROOM;

        [SerializeField]
        private int targetId_;

        private int parentTargetId_ = -1;

        [SerializeField]
        private GameObject visibilityRoot_;

        private bool dropEnabled_ = true;
        private bool interactionVisible_;
        private bool subscribed_;

        public InteractionPointType SupportedInventoryType => supportedInventoryType_;
        public RoomTargetKind RoomTargetKind => roomTargetKind_;
        public int TargetId => targetId_;
        public int ParentTargetId => parentTargetId_;

        [Inject]
        public void Construct(EventDispatcher dispatcher, RoomSettings roomSettings)
        {
            dispatcher_ = dispatcher;
            roomSettings_ = roomSettings;
        }

        private void Awake()
        {
            Configure(
                reparentDroppedItem: false,
                resetAnchoredPositionOnDrop: true,
                positionDroppedItemAtPointer: false);

            SubscribeToDispatcher();
            interactionVisible_ = false;
            UpdateVisibility();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        public override bool CanAccept(UIDraggable draggable)
        {
            return interactionVisible_ && dropEnabled_ && HasValidTargetConfiguration() && base.CanAccept(draggable);
        }

        public void SetTargetIds(int targetId, int parentTargetId = -1)
        {
            targetId_ = targetId;
            parentTargetId_ = parentTargetId;
            RefreshVisibility();
        }

        public void SetTargetId(int targetId)
        {
            targetId_ = targetId;
            RefreshVisibility();
        }

        public void SetParentTargetId(int parentTargetId)
        {
            parentTargetId_ = parentTargetId;
            RefreshVisibility();
        }

        public void SetDropEnabled(bool enabled)
        {
            dropEnabled_ = enabled;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            UpdateVisibility();
        }

        private bool ShouldShowForInteractionType(InteractionPointType? interactionPointType)
        {
            return interactionPointType == SupportedInventoryType;
        }

        private bool HasValidTargetConfiguration()
        {
            switch (roomTargetKind_)
            {
                case RoomTargetKind.ROOM:
                    return true;
                case RoomTargetKind.PLACEABLE_OBJECT:
                    if (supportedInventoryType_ == InteractionPointType.PAINT && parentTargetId_ < 0)
                    {
                        return true;
                    }

                    return targetId_ > 0;
                default:
                    return false;
            }
        }

        private void OnRoomInventoryDragStarted(RoomInventoryDragStartedEvent eventData)
        {
            interactionVisible_ = eventData?.Data != null &&
                ShouldShowForInteractionType(eventData.Data.InteractionPointType) &&
                CanShowForDraggedItem(eventData.Data);
            UpdateVisibility();
        }

        private bool CanShowForDraggedItem(RoomInventoryItemData data)
        {
            if (data == null)
            {
                return false;
            }

            if (supportedInventoryType_ != InteractionPointType.PLACEABLE_OBJECT ||
                roomTargetKind_ != RoomTargetKind.PLACEABLE_OBJECT)
            {
                return true;
            }

            return roomSettings_ == null || !roomSettings_.SupportsChildPlaceables(data.ItemId);
        }

        private void OnRoomInventoryDragEnded(RoomInventoryDragEndedEvent _)
        {
            interactionVisible_ = false;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (visibilityRoot_ != null)
            {
                visibilityRoot_.SetActive(interactionVisible_ && dropEnabled_ && HasValidTargetConfiguration());
            }
        }

        private void SubscribeToDispatcher()
        {
            if (subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Subscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            subscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Unsubscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            subscribed_ = false;
        }
    }
}
