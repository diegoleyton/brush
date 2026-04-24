using Flowbit.Utilities.Core.Events;

using Game.Core.Data;

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
    /// Requested when paint should be applied to a placed room object.
    /// </summary>
    public sealed class RoomObjectPaintAppliedEvent : IEvent
    {
        public RoomObjectPaintAppliedEvent(int itemId, int locationId)
        {
            ItemId = itemId;
            LocationId = locationId;
        }

        public int ItemId { get; }
        public int LocationId { get; }
    }

    /// <summary>
    /// Requested when paint should be applied to a placed child room object.
    /// </summary>
    public sealed class RoomChildObjectPaintAppliedEvent : IEvent
    {
        public RoomChildObjectPaintAppliedEvent(int itemId, int locationId, int slotId)
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
    /// Requested when food should be applied from the room scene.
    /// </summary>
    public sealed class RoomFoodAppliedEvent : IEvent
    {
        public RoomFoodAppliedEvent(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }

    /// <summary>
    /// Requested when a pet skin should be applied from the room scene.
    /// </summary>
    public sealed class RoomSkinAppliedEvent : IEvent
    {
        public RoomSkinAppliedEvent(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }

    /// <summary>
    /// Requested when a pet hat should be applied from the room scene.
    /// </summary>
    public sealed class RoomHatAppliedEvent : IEvent
    {
        public RoomHatAppliedEvent(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }

    /// <summary>
    /// Requested when a pet dress should be applied from the room scene.
    /// </summary>
    public sealed class RoomDressAppliedEvent : IEvent
    {
        public RoomDressAppliedEvent(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }

    /// <summary>
    /// Requested when a pet face should be applied from the room scene.
    /// </summary>
    public sealed class RoomFaceAppliedEvent : IEvent
    {
        public RoomFaceAppliedEvent(int itemId)
        {
            ItemId = itemId;
        }

        public int ItemId { get; }
    }

    /// <summary>
    /// Emitted by the data layer after a room item operation is successfully applied.
    /// </summary>
    public sealed class RoomDataItemAppliedEvent : IEvent
    {
        public RoomDataItemAppliedEvent(
            InteractionPointType itemType,
            int itemId,
            int targetId,
            int parentTargetId = -1)
        {
            ItemType = itemType;
            ItemId = itemId;
            TargetId = targetId;
            ParentTargetId = parentTargetId;
        }

        public InteractionPointType ItemType { get; }
        public int ItemId { get; }
        public int TargetId { get; }
        public int ParentTargetId { get; }
    }

    /// <summary>
    /// Emitted by the data layer when a room item operation could not be applied.
    /// </summary>
    public sealed class RoomDataItemApplyFailedEvent : IEvent
    {
        public RoomDataItemApplyFailedEvent(
            InteractionPointType itemType,
            int itemId,
            int targetId,
            int parentTargetId = -1)
        {
            ItemType = itemType;
            ItemId = itemId;
            TargetId = targetId;
            ParentTargetId = parentTargetId;
        }

        public InteractionPointType ItemType { get; }
        public int ItemId { get; }
        public int TargetId { get; }
        public int ParentTargetId { get; }
    }

    /// <summary>
    /// Emitted by the data layer after pet data is successfully updated.
    /// </summary>
    public sealed class PetDataAppliedEvent : IEvent
    {
        public PetDataAppliedEvent(InteractionPointType itemType, int itemId)
        {
            ItemType = itemType;
            ItemId = itemId;
        }

        public InteractionPointType ItemType { get; }
        public int ItemId { get; }
    }

    /// <summary>
    /// Emitted by the data layer when pet data could not be updated.
    /// </summary>
    public sealed class PetDataApplyFailedEvent : IEvent
    {
        public PetDataApplyFailedEvent(InteractionPointType itemType, int itemId)
        {
            ItemType = itemType;
            ItemId = itemId;
        }

        public InteractionPointType ItemType { get; }
        public int ItemId { get; }
    }
}
