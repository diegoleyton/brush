using System.Collections.Generic;

using Flowbit.Utilities.Core.Events;

using Game.Core.Data;
using Game.Unity.Definitions;
using Game.Unity.Definitions.Events;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Tracks the latest confirmed drop positions used by visual effects that play after data confirmation.
    /// </summary>
    public sealed class InventoryDropEffectPositionTracker : System.IDisposable
    {
        private readonly EventDispatcher dispatcher_;
        private readonly Dictionary<(InteractionPointType, RoomTargetKind), Vector2> positions_ =
            new Dictionary<(InteractionPointType, RoomTargetKind), Vector2>();

        public InventoryDropEffectPositionTracker(EventDispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
            SubscribeToDispatcher();
        }

        public void Dispose()
        {
            UnsubscribeFromDispatcher();
        }

        public Vector2? Consume(InteractionPointType itemType, RoomTargetKind targetKind)
        {
            if (!positions_.TryGetValue((itemType, targetKind), out Vector2 position))
            {
                return null;
            }

            positions_.Remove((itemType, targetKind));
            return position;
        }

        public void Clear(InteractionPointType itemType, RoomTargetKind targetKind)
        {
            positions_.Remove((itemType, targetKind));
        }

        private void OnRoomPaintDropPositionCaptured(RoomPaintDropPositionCapturedEvent eventData)
        {
            if (eventData == null)
            {
                return;
            }

            positions_[(InteractionPointType.PAINT, RoomTargetKind.ROOM)] = eventData.DropScreenPosition;
        }

        private void OnPetSkinDropPositionCaptured(PetSkinDropPositionCapturedEvent eventData)
        {
            if (eventData == null)
            {
                return;
            }

            positions_[(InteractionPointType.SKIN, RoomTargetKind.ROOM)] = eventData.DropScreenPosition;
        }

        private void OnPetFoodDropPositionCaptured(PetFoodDropPositionCapturedEvent eventData)
        {
            if (eventData == null)
            {
                return;
            }

            positions_[(InteractionPointType.FOOD, RoomTargetKind.ROOM)] = eventData.DropScreenPosition;
        }

        private void SubscribeToDispatcher()
        {
            if (dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<RoomPaintDropPositionCapturedEvent>(OnRoomPaintDropPositionCaptured);
            dispatcher_.Subscribe<PetSkinDropPositionCapturedEvent>(OnPetSkinDropPositionCaptured);
            dispatcher_.Subscribe<PetFoodDropPositionCapturedEvent>(OnPetFoodDropPositionCaptured);
        }

        private void UnsubscribeFromDispatcher()
        {
            if (dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomPaintDropPositionCapturedEvent>(OnRoomPaintDropPositionCaptured);
            dispatcher_.Unsubscribe<PetSkinDropPositionCapturedEvent>(OnPetSkinDropPositionCaptured);
            dispatcher_.Unsubscribe<PetFoodDropPositionCapturedEvent>(OnPetFoodDropPositionCaptured);
        }
    }
}
