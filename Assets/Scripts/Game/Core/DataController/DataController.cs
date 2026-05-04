using Flowbit.Utilities.Core.Events;

using Game.Core.Events;
using Game.Core.Services;

namespace Game.Core.DataController
{
    /// <summary>
    /// Listens to room data events and forwards them to the game data repository.
    /// </summary>
    public sealed class DataController : System.IDisposable
    {
        private readonly IRoomGameplayService roomGameplayService_;
        private readonly EventDispatcher dispatcher_;
        private bool disposed_;

        /// <summary>
        /// Creates a new data controller and starts listening for room data events.
        /// </summary>
        public DataController(IRoomGameplayService roomGameplayService, EventDispatcher dispatcher)
        {
            roomGameplayService_ = roomGameplayService;
            dispatcher_ = dispatcher;

            dispatcher_?.Subscribe<RoomObjectPlacedEvent>(OnRoomObjectPlaced);
            dispatcher_?.Subscribe<RoomObjectMovedEvent>(OnRoomObjectMoved);
            dispatcher_?.Subscribe<RoomObjectReturnedToInventoryEvent>(OnRoomObjectReturnedToInventory);
            dispatcher_?.Subscribe<RoomPaintAppliedEvent>(OnRoomPaintApplied);
            dispatcher_?.Subscribe<RoomFoodAppliedEvent>(OnRoomFoodApplied);
            dispatcher_?.Subscribe<RoomSkinAppliedEvent>(OnRoomSkinApplied);
            dispatcher_?.Subscribe<RoomHatAppliedEvent>(OnRoomHatApplied);
            dispatcher_?.Subscribe<RoomDressAppliedEvent>(OnRoomDressApplied);
            dispatcher_?.Subscribe<RoomEyesAppliedEvent>(OnRoomEyesApplied);
        }

        /// <summary>
        /// Stops listening for room data events.
        /// </summary>
        public void Dispose()
        {
            if (disposed_)
            {
                return;
            }

            dispatcher_?.Unsubscribe<RoomObjectPlacedEvent>(OnRoomObjectPlaced);
            dispatcher_?.Unsubscribe<RoomObjectMovedEvent>(OnRoomObjectMoved);
            dispatcher_?.Unsubscribe<RoomObjectReturnedToInventoryEvent>(OnRoomObjectReturnedToInventory);
            dispatcher_?.Unsubscribe<RoomPaintAppliedEvent>(OnRoomPaintApplied);
            dispatcher_?.Unsubscribe<RoomFoodAppliedEvent>(OnRoomFoodApplied);
            dispatcher_?.Unsubscribe<RoomSkinAppliedEvent>(OnRoomSkinApplied);
            dispatcher_?.Unsubscribe<RoomHatAppliedEvent>(OnRoomHatApplied);
            dispatcher_?.Unsubscribe<RoomDressAppliedEvent>(OnRoomDressApplied);
            dispatcher_?.Unsubscribe<RoomEyesAppliedEvent>(OnRoomEyesApplied);
            disposed_ = true;
        }

        private void OnRoomObjectPlaced(RoomObjectPlacedEvent eventData)
        {
            roomGameplayService_?.SetRoomObject(eventData.TargetId, eventData.ItemId);
        }

        private void OnRoomPaintApplied(RoomPaintAppliedEvent eventData)
        {
            roomGameplayService_?.PaintRoomSurface(eventData.TargetId, eventData.ItemId);
        }

        private void OnRoomObjectMoved(RoomObjectMovedEvent eventData)
        {
            roomGameplayService_?.MoveRoomObject(eventData.SourceTargetId, eventData.TargetId);
        }

        private void OnRoomObjectReturnedToInventory(RoomObjectReturnedToInventoryEvent eventData)
        {
            roomGameplayService_?.ReturnRoomObjectToInventory(eventData.SourceTargetId);
        }

        private void OnRoomFoodApplied(RoomFoodAppliedEvent eventData)
        {
            roomGameplayService_?.FeedPet(eventData.ItemId);
        }

        private void OnRoomSkinApplied(RoomSkinAppliedEvent eventData)
        {
            roomGameplayService_?.SetPetSkin(eventData.ItemId);
        }

        private void OnRoomHatApplied(RoomHatAppliedEvent eventData)
        {
            roomGameplayService_?.SetPetHat(eventData.ItemId);
        }

        private void OnRoomDressApplied(RoomDressAppliedEvent eventData)
        {
            roomGameplayService_?.SetPetDress(eventData.ItemId);
        }

        private void OnRoomEyesApplied(RoomEyesAppliedEvent eventData)
        {
            roomGameplayService_?.SetPetEyes(eventData.ItemId);
        }
    }
}
