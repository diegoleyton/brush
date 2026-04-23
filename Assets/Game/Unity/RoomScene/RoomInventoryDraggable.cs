using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.DragAndDrop;

using Game.Unity.Definitions.Events;

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

        public override void OnDropAccepted(UIDropTarget target)
        {
            if (dispatcher_ == null || data_ == null)
            {
                return;
            }

            if (target is RoomObjectDropArea dropArea)
            {
                dispatcher_.Send(new RoomInventoryDropAcceptedEvent(dropArea, data_));
                return;
            }

            if (target is RoomChildDropArea childDropArea)
            {
                dispatcher_.Send(new RoomInventoryChildDropAcceptedEvent(childDropArea, data_));
                return;
            }
        }
    }
}
