using Flowbit.Utilities.Core.Events;

using Game.Unity.RoomScene;

namespace Game.Unity.Definitions.Events
{
    /// <summary>
    /// Emitted when dragging starts for a room inventory item.
    /// </summary>
    public sealed class RoomInventoryDragStartedEvent : IEvent
    {
        public RoomInventoryDragStartedEvent(RoomInventoryItemData data)
        {
            Data = data;
        }

        public RoomInventoryItemData Data { get; }
    }

    /// <summary>
    /// Emitted when dragging ends for a room inventory item.
    /// </summary>
    public sealed class RoomInventoryDragEndedEvent : IEvent
    {
        public RoomInventoryDragEndedEvent(RoomInventoryItemData data, bool dropAccepted)
        {
            Data = data;
            DropAccepted = dropAccepted;
        }

        public RoomInventoryItemData Data { get; }
        public bool DropAccepted { get; }
    }

    /// <summary>
    /// Emitted when a room inventory item is accepted by a room drop area.
    /// </summary>
    public sealed class RoomInventoryDropAcceptedEvent : IEvent
    {
        public RoomInventoryDropAcceptedEvent(RoomDropArea dropArea, RoomInventoryItemData data)
        {
            DropArea = dropArea;
            Data = data;
        }

        public RoomDropArea DropArea { get; }
        public RoomInventoryItemData Data { get; }
    }
}
