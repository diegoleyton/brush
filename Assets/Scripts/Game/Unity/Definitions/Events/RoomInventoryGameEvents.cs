using Flowbit.Utilities.Core.Events;

using Game.Unity.RoomScene;

using UnityEngine;

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
        public RoomInventoryDropAcceptedEvent(RoomDropArea dropArea, RoomInventoryItemData data, Vector2 dropScreenPosition)
        {
            DropArea = dropArea;
            Data = data;
            DropScreenPosition = dropScreenPosition;
        }

        public RoomDropArea DropArea { get; }
        public RoomInventoryItemData Data { get; }
        public Vector2 DropScreenPosition { get; }
    }

    /// <summary>
    /// Emitted immediately before a room paint request is sent, capturing the latest drop screen position.
    /// </summary>
    public sealed class RoomPaintDropPositionCapturedEvent : IEvent
    {
        public RoomPaintDropPositionCapturedEvent(Vector2 dropScreenPosition)
        {
            DropScreenPosition = dropScreenPosition;
        }

        public Vector2 DropScreenPosition { get; }
    }

    /// <summary>
    /// Emitted immediately before a pet skin request is sent, capturing the latest drop screen position.
    /// </summary>
    public sealed class PetSkinDropPositionCapturedEvent : IEvent
    {
        public PetSkinDropPositionCapturedEvent(Vector2 dropScreenPosition)
        {
            DropScreenPosition = dropScreenPosition;
        }

        public Vector2 DropScreenPosition { get; }
    }

    /// <summary>
    /// Emitted immediately before a pet food request is sent, capturing the latest drop screen position.
    /// </summary>
    public sealed class PetFoodDropPositionCapturedEvent : IEvent
    {
        public PetFoodDropPositionCapturedEvent(Vector2 dropScreenPosition)
        {
            DropScreenPosition = dropScreenPosition;
        }

        public Vector2 DropScreenPosition { get; }
    }

    /// <summary>
    /// Emitted when the pet finishes its eat presentation sequence.
    /// </summary>
    public sealed class PetFoodAnimationCompletedEvent : IEvent
    {
    }

    /// <summary>
    /// Emitted when the room paint surface effect finishes its presentation.
    /// </summary>
    public sealed class RoomPaintEffectCompletedEvent : IEvent
    {
        public RoomPaintEffectCompletedEvent(int surfaceId)
        {
            SurfaceId = surfaceId;
        }

        public int SurfaceId { get; }
    }

    /// <summary>
    /// Emitted when the pet skin effect finishes its presentation.
    /// </summary>
    public sealed class PetSkinEffectCompletedEvent : IEvent
    {
    }
}
