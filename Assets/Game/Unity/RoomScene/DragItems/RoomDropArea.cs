using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.DragAndDrop;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Events;
using Game.Unity.Definitions;

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
        private IRoomInventorySelectionState selectionState_;

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
        public void Construct(EventDispatcher dispatcher, IRoomInventorySelectionState selectionState)
        {
            dispatcher_ = dispatcher;
            selectionState_ = selectionState;
        }

        private void Awake()
        {
            Configure(
                reparentDroppedItem: false,
                resetAnchoredPositionOnDrop: true,
                positionDroppedItemAtPointer: false);

            SubscribeToDispatcher();
            interactionVisible_ = ShouldShowForInteractionType(selectionState_?.CurrentInteractionPointType);
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
                    if (supportedInventoryType_ == InteractionPointType.PAINT)
                    {
                        return GameIds.IsRoomSurfaceId(targetId_);
                    }

                    return GameIds.IsRoomObjectLocationId(targetId_);
                case RoomTargetKind.PLACEABLE_OBJECT:
                    if (supportedInventoryType_ == InteractionPointType.PAINT && parentTargetId_ < 0)
                    {
                        return GameIds.IsRoomObjectLocationId(targetId_);
                    }

                    return GameIds.IsRoomObjectLocationId(parentTargetId_) && targetId_ > 0;
                default:
                    return false;
            }
        }

        private void OnShowInteractionPoints(ShowInteractionPointsEvent eventData)
        {
            interactionVisible_ = ShouldShowForInteractionType(eventData.InteractionPointType);
            UpdateVisibility();
        }

        private void OnHideInteractionPoints(HideInteractionPointsEvent _)
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

            dispatcher_.Subscribe<ShowInteractionPointsEvent>(OnShowInteractionPoints);
            dispatcher_.Subscribe<HideInteractionPointsEvent>(OnHideInteractionPoints);
            subscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<ShowInteractionPointsEvent>(OnShowInteractionPoints);
            dispatcher_.Unsubscribe<HideInteractionPointsEvent>(OnHideInteractionPoints);
            subscribed_ = false;
        }
    }
}
