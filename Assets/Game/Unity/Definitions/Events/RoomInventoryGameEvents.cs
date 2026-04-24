using Flowbit.Utilities.Core.Events;

using Game.Unity.RoomScene;

namespace Game.Unity.Definitions.Events
{
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
