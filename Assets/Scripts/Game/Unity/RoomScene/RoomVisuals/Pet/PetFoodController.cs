using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.ScreenBlocker;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions;
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
        private readonly IRoomGameplayService roomGameplayService_;
        private readonly InventoryDropEffectPositionTracker dropEffectPositionTracker_;
        private readonly ScreenBlocker screenBlocker_;

        private bool isFoodDragActive_;
        private bool foodDropAccepted_;
        private bool foodDragEnded_;
        private int foodItemId_;
        private FoodApplyOutcome foodApplyOutcome_;
        private System.IDisposable eatBlockScope_;

        public PetFoodController(
            PetView petView,
            EventDispatcher dispatcher,
            IRoomGameplayService roomGameplayService,
            InventoryDropEffectPositionTracker dropEffectPositionTracker,
            ScreenBlocker screenBlocker)
        {
            petView_ = petView;
            dispatcher_ = dispatcher;
            roomGameplayService_ = roomGameplayService;
            dropEffectPositionTracker_ = dropEffectPositionTracker;
            screenBlocker_ = screenBlocker;
            if (petView_ != null)
            {
                petView_.EatAnimationCompleted += OnEatAnimationCompleted;
            }
            SubscribeToDispatcher();
        }

        public void Dispose()
        {
            DisposeEatBlockScope();

            if (petView_ != null)
            {
                petView_.EatAnimationCompleted -= OnEatAnimationCompleted;
            }

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

            if (petView_ == null || roomGameplayService_ == null)
            {
                return;
            }

            petView_.PrepareToEat(roomGameplayService_.CanPetEat);
        }

        private void OnRoomInventoryDropAccepted(RoomInventoryDropAcceptedEvent eventData)
        {
            if (!IsFoodEvent(eventData?.Data))
            {
                return;
            }

            foodDropAccepted_ = true;
            foodItemId_ = eventData.Data.ItemId;
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
                    DisposeEatBlockScope();
                    eatBlockScope_ = screenBlocker_?.BlockScope("PetEatAnimation");
                    petView_?.Eat(
                        foodItemId_,
                        dropEffectPositionTracker_?.Consume(InteractionPointType.FOOD));
                    ResetFoodState();
                    return;
                case FoodApplyOutcome.Failed:
                    DisposeEatBlockScope();
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
            foodItemId_ = 0;
            foodApplyOutcome_ = FoodApplyOutcome.None;
        }

        private void OnEatAnimationCompleted()
        {
            DisposeEatBlockScope();
            dispatcher_?.Send(new PetFoodAnimationCompletedEvent());
        }

        private void DisposeEatBlockScope()
        {
            if (eatBlockScope_ == null)
            {
                return;
            }

            eatBlockScope_.Dispose();
            eatBlockScope_ = null;
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
