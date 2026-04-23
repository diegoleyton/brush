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
        private readonly DataRepository repository_;
        private readonly EventDispatcher dispatcher_;
        private bool disposed_;

        /// <summary>
        /// Creates a new data controller and starts listening for room data events.
        /// </summary>
        public DataController(DataRepository repository, EventDispatcher dispatcher)
        {
            repository_ = repository;
            dispatcher_ = dispatcher;

            dispatcher_?.Subscribe<RoomObjectPlacedEvent>(OnRoomObjectPlaced);
            dispatcher_?.Subscribe<RoomPaintAppliedEvent>(OnRoomPaintApplied);
            dispatcher_?.Subscribe<RoomFoodAppliedEvent>(OnRoomFoodApplied);
            dispatcher_?.Subscribe<RoomSkinAppliedEvent>(OnRoomSkinApplied);
            dispatcher_?.Subscribe<RoomFaceAppliedEvent>(OnRoomFaceApplied);
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
            dispatcher_?.Unsubscribe<RoomPaintAppliedEvent>(OnRoomPaintApplied);
            dispatcher_?.Unsubscribe<RoomFoodAppliedEvent>(OnRoomFoodApplied);
            dispatcher_?.Unsubscribe<RoomSkinAppliedEvent>(OnRoomSkinApplied);
            dispatcher_?.Unsubscribe<RoomFaceAppliedEvent>(OnRoomFaceApplied);
            disposed_ = true;
        }

        private void OnRoomObjectPlaced(RoomObjectPlacedEvent eventData)
        {
            repository_?.SetRoomObject(eventData.TargetId, eventData.ItemId);
        }

        private void OnRoomPaintApplied(RoomPaintAppliedEvent eventData)
        {
            repository_?.PaintRoomSurface(eventData.TargetId, eventData.ItemId);
        }

        private void OnRoomFoodApplied(RoomFoodAppliedEvent eventData)
        {
            repository_?.FeedPet(eventData.ItemId);
        }

        private void OnRoomSkinApplied(RoomSkinAppliedEvent eventData)
        {
            repository_?.SetPetSkin(eventData.ItemId);
        }

        private void OnRoomFaceApplied(RoomFaceAppliedEvent eventData)
        {
            repository_?.SetPetFace(eventData.ItemId);
        }
    }
}
