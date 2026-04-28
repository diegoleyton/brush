using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.DragAndDrop;

using Game.Unity.Definitions.Events;

using UnityEngine.EventSystems;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Room-specific draggable that emits a room-scene event when its drop is accepted.
    /// </summary>
    public sealed class RoomInventoryDraggable : UIDraggable
    {
        private EventDispatcher dispatcher_;

        private RoomInventoryItemData data_;
        private RoomInventoryItemData activeDragData_;

        [Inject]
        public void Construct(EventDispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        public void SetData(RoomInventoryItemData data)
        {
            data_ = data;
        }

        public void ClearData()
        {
            data_ = null;
        }

        public override void OnDropAccepted(UIDropTarget target, PointerEventData eventData)
        {
            RoomInventoryItemData dragData = activeDragData_ ?? data_;
            if (dispatcher_ == null || dragData == null)
            {
                return;
            }

            if (target is RoomDropArea dropArea)
            {
                dispatcher_.Send(new RoomInventoryDropAcceptedEvent(dropArea, dragData, eventData.position));
            }
        }

        public override void OnDragStarted()
        {
            if (dispatcher_ == null || data_ == null)
            {
                return;
            }

            activeDragData_ = data_;
            dispatcher_.Send(new RoomInventoryDragStartedEvent(activeDragData_));
        }

        public override void OnDragEnded(bool dropAccepted)
        {
            RoomInventoryItemData dragData = activeDragData_ ?? data_;
            if (dispatcher_ == null || dragData == null)
            {
                return;
            }

            dispatcher_.Send(new RoomInventoryDragEndedEvent(dragData, dropAccepted));
            activeDragData_ = null;
        }
    }
}
