using Flowbit.Utilities.Core.Events;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions.Events;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Coordinates pet food reactions based on drag-and-drop and confirmed data results.
    /// </summary>
    public sealed class PetFoodController : System.IDisposable
    {
        private enum FoodApplyOutcome
        {
            None,
            Success,
            Failed
        }

        private readonly PetView petView_;
        private readonly EventDispatcher dispatcher_;
        private readonly DataRepository repository_;

        private bool isFoodDragActive_;
        private bool foodDropAccepted_;
        private bool foodDragEnded_;
        private FoodApplyOutcome foodApplyOutcome_;

        public PetFoodController(PetView petView, EventDispatcher dispatcher, DataRepository repository)
        {
            petView_ = petView;
            dispatcher_ = dispatcher;
            repository_ = repository;
            SubscribeToDispatcher();
        }

        public void Dispose()
        {
            UnsubscribeFromDispatcher();
        }

        private void OnRoomInventoryDragStarted(RoomInventoryDragStartedEvent eventData)
        {
            if (eventData?.Data == null ||
                eventData.Data.InteractionPointType != InteractionPointType.FOOD)
            {
                return;
            }

            ResetFoodState();
            isFoodDragActive_ = true;

            if (petView_ == null || repository_ == null)
            {
                return;
            }

            petView_.PrepareToEat(repository_.CanPetEat);
        }

        private void OnRoomInventoryDropAccepted(RoomInventoryDropAcceptedEvent eventData)
        {
            if (!IsFoodEvent(eventData?.Data))
            {
                return;
            }

            foodDropAccepted_ = true;
            TryFinalizeFoodInteraction();
        }

        private void OnRoomInventoryDragEnded(RoomInventoryDragEndedEvent eventData)
        {
            if (!IsFoodEvent(eventData?.Data) || !isFoodDragActive_)
            {
                return;
            }

            foodDragEnded_ = true;

            if (!eventData.DropAccepted)
            {
                petView_?.ExitFoodState();
                ResetFoodState();
                return;
            }

            TryFinalizeFoodInteraction();
        }

        private void OnPetDataApplied(PetDataAppliedEvent eventData)
        {
            if (eventData == null ||
                eventData.ItemType != InteractionPointType.FOOD ||
                !isFoodDragActive_)
            {
                return;
            }

            foodApplyOutcome_ = FoodApplyOutcome.Success;
            TryFinalizeFoodInteraction();
        }

        private void OnPetDataApplyFailed(PetDataApplyFailedEvent eventData)
        {
            if (eventData == null ||
                eventData.ItemType != InteractionPointType.FOOD ||
                !isFoodDragActive_)
            {
                return;
            }

            foodApplyOutcome_ = FoodApplyOutcome.Failed;
            TryFinalizeFoodInteraction();
        }

        private void TryFinalizeFoodInteraction()
        {
            if (!isFoodDragActive_ || !foodDropAccepted_ || !foodDragEnded_)
            {
                return;
            }

            switch (foodApplyOutcome_)
            {
                case FoodApplyOutcome.Success:
                    petView_?.Eat();
                    ResetFoodState();
                    return;
                case FoodApplyOutcome.Failed:
                    petView_?.ExitFoodState();
                    ResetFoodState();
                    return;
            }
        }

        private void ResetFoodState()
        {
            isFoodDragActive_ = false;
            foodDropAccepted_ = false;
            foodDragEnded_ = false;
            foodApplyOutcome_ = FoodApplyOutcome.None;
        }

        private bool IsFoodEvent(RoomInventoryItemData data)
        {
            return data != null && data.InteractionPointType == InteractionPointType.FOOD;
        }

        private void SubscribeToDispatcher()
        {
            if (dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Subscribe<RoomInventoryDropAcceptedEvent>(OnRoomInventoryDropAccepted);
            dispatcher_.Subscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            dispatcher_.Subscribe<PetDataAppliedEvent>(OnPetDataApplied);
            dispatcher_.Subscribe<PetDataApplyFailedEvent>(OnPetDataApplyFailed);
        }

        private void UnsubscribeFromDispatcher()
        {
            if (dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Unsubscribe<RoomInventoryDropAcceptedEvent>(OnRoomInventoryDropAccepted);
            dispatcher_.Unsubscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            dispatcher_.Unsubscribe<PetDataAppliedEvent>(OnPetDataApplied);
            dispatcher_.Unsubscribe<PetDataApplyFailedEvent>(OnPetDataApplyFailed);
        }

        public sealed class Factory : PlaceholderFactory<PetView, PetFoodController>
        {
        }
    }
}
