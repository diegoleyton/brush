using Flowbit.Utilities.Core.Events;

using Game.Core.Data;
using Game.Core.Events;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Read-only access to the currently selected room inventory category.
    /// </summary>
    public interface IRoomInventorySelectionState
    {
        InteractionPointType CurrentInteractionPointType { get; }
    }

    /// <summary>
    /// Stores the currently selected room inventory category so scene views can query it at any time.
    /// </summary>
    public sealed class RoomInventorySelectionState : IRoomInventorySelectionState
    {
        private readonly EventDispatcher dispatcher_;

        public InteractionPointType CurrentInteractionPointType { get; private set; } =
            InteractionPointType.PLACEABLE_OBJECT;

        public RoomInventorySelectionState(EventDispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        public void SetCurrentInteractionPointType(InteractionPointType interactionPointType)
        {
            if (CurrentInteractionPointType == interactionPointType)
            {
                return;
            }

            CurrentInteractionPointType = interactionPointType;
            dispatcher_?.Send(new SwitchListEvent(interactionPointType));
        }
    }
}
