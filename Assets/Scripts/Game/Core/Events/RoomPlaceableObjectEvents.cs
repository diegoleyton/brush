using Flowbit.Utilities.Core.Events;

namespace Game.Core.Events
{
    /// <summary>
    /// Requested when a placed room object should move from one room slot to another.
    /// </summary>
    public sealed class RoomObjectMovedEvent : IEvent
    {
        public RoomObjectMovedEvent(int itemId, int sourceTargetId, int targetId)
        {
            ItemId = itemId;
            SourceTargetId = sourceTargetId;
            TargetId = targetId;
        }

        public int ItemId { get; }
        public int SourceTargetId { get; }
        public int TargetId { get; }
    }

    /// <summary>
    /// Requested when a placed room object should be removed from the room and returned to inventory.
    /// </summary>
    public sealed class RoomObjectReturnedToInventoryEvent : IEvent
    {
        public RoomObjectReturnedToInventoryEvent(int itemId, int sourceTargetId)
        {
            ItemId = itemId;
            SourceTargetId = sourceTargetId;
        }

        public int ItemId { get; }
        public int SourceTargetId { get; }
    }
}
