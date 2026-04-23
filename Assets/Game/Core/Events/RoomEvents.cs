using Flowbit.Utilities.Core.Events;

namespace Game.Core.Events
{
    /// <summary>
    /// Requested when a room object should be placed in a room target.
    /// </summary>
    public sealed class RoomObjectPlacedEvent : IEvent
    {
        public RoomObjectPlacedEvent(int itemId, int targetId)
        {
            ItemId = itemId;
            TargetId = targetId;
        }

        public int ItemId { get; }
        public int TargetId { get; }
    }

    /// <summary>
    /// Requested when a child room object should be placed in a parent slot.
    /// </summary>
    public sealed class RoomChildObjectPlacedEvent : IEvent
    {
        public RoomChildObjectPlacedEvent(int itemId, int locationId, int slotId)
        {
            ItemId = itemId;
            LocationId = locationId;
            SlotId = slotId;
        }

        public int ItemId { get; }
        public int LocationId { get; }
        public int SlotId { get; }
    }

    /// <summary>
    /// Requested when paint should be applied to a room target.
    /// </summary>
    public sealed class RoomPaintAppliedEvent : IEvent
    {
        public RoomPaintAppliedEvent(int itemId, int targetId)
        {
            ItemId = itemId;
            TargetId = targetId;
        }

        public int ItemId { get; }
        public int TargetId { get; }
    }

    /// <summary>
    /// Requested when food should be applied from the room scene.
    /// </summary>
    public sealed class RoomFoodAppliedEvent : IEvent
    {
        public RoomFoodAppliedEvent(int itemId, int targetId)
        {
            ItemId = itemId;
            TargetId = targetId;
        }

        public int ItemId { get; }
        public int TargetId { get; }
    }

    /// <summary>
    /// Requested when a pet skin should be applied from the room scene.
    /// </summary>
    public sealed class RoomSkinAppliedEvent : IEvent
    {
        public RoomSkinAppliedEvent(int itemId, int targetId)
        {
            ItemId = itemId;
            TargetId = targetId;
        }

        public int ItemId { get; }
        public int TargetId { get; }
    }

    /// <summary>
    /// Requested when a pet face should be applied from the room scene.
    /// </summary>
    public sealed class RoomFaceAppliedEvent : IEvent
    {
        public RoomFaceAppliedEvent(int itemId, int targetId)
        {
            ItemId = itemId;
            TargetId = targetId;
        }

        public int ItemId { get; }
        public int TargetId { get; }
    }
}
