using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.DragAndDrop;

using Game.Core.Events;
using Game.Unity.Definitions.Events;

using UnityEngine.EventSystems;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Draggable behavior for a placeable object that is already positioned in the room.
    /// </summary>
    public sealed class RoomPlacedObjectDraggable : UIDraggable
    {
        private EventDispatcher dispatcher_;
        private RoomPlaceableObjectView placeableObjectView_;
        private RoomInventoryItemData data_;
        private int sourceLocationId_;

        public void Configure(
            EventDispatcher dispatcher,
            RoomPlaceableObjectView placeableObjectView,
            RoomInventoryItemData data,
            int sourceLocationId)
        {
            dispatcher_ = dispatcher;
            placeableObjectView_ = placeableObjectView;
            data_ = data;
            sourceLocationId_ = sourceLocationId;
        }

        public override void OnDragStarted()
        {
            if (dispatcher_ == null || data_ == null)
            {
                return;
            }

            placeableObjectView_?.SetVisualVisible(false);
            dispatcher_.Send(new RoomInventoryDragStartedEvent(data_, sourceLocationId_));
        }

        public override void OnDropAccepted(UIDropTarget target, PointerEventData eventData)
        {
            if (dispatcher_ == null || data_ == null)
            {
                return;
            }

            if (target is RoomDropArea dropArea)
            {
                dispatcher_.Send(new RoomObjectMovedEvent(data_.ItemId, sourceLocationId_, dropArea.TargetId));
            }
        }

        public override void OnDragEnded(bool dropAccepted)
        {
            if (dispatcher_ == null || data_ == null)
            {
                return;
            }

            if (!dropAccepted && !WasCancelledByCancelArea)
            {
                placeableObjectView_?.SetVisualVisible(true);
            }

            if (!dropAccepted && WasCancelledByCancelArea)
            {
                dispatcher_.Send(new RoomObjectReturnedToInventoryEvent(data_.ItemId, sourceLocationId_));
            }

            dispatcher_.Send(new RoomInventoryDragEndedEvent(data_, dropAccepted));
        }
    }
}
