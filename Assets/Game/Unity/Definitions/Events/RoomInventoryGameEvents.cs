using Flowbit.Utilities.Core.Events;

using Game.Unity.RoomScene;

namespace Game.Unity.Definitions.Events
{
    /// <summary>
    /// Emitted when a room inventory item is accepted by a room drop area.
    /// </summary>
    public sealed class RoomInventoryDropAcceptedEvent : IEvent
    {
        public RoomInventoryDropAcceptedEvent(RoomObjectDropArea dropArea, RoomInventoryItemData data)
        {
            DropArea = dropArea;
            Data = data;
        }

        public RoomObjectDropArea DropArea { get; }
        public RoomInventoryItemData Data { get; }
    }
}
