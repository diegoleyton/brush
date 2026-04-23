using Flowbit.Utilities.Core.Events;

using Game.Core.Data;

namespace Game.Core.Events
{
    /// <summary>
    /// Requests that the UI shows available interaction points for a given item type.
    /// </summary>
    public sealed class ShowInteractionPointsEvent : IEvent
    {
        public ShowInteractionPointsEvent(InteractionPointType interactionPointType)
        {
            InteractionPointType = interactionPointType;
        }

        public InteractionPointType InteractionPointType { get; }
    }

    /// <summary>
    /// Requests that the UI hides all currently visible interaction points.
    /// </summary>
    public sealed class HideInteractionPointsEvent : IEvent
    {
    }

    /// <summary>
    /// Requests that the active inventory list switches to a new category.
    /// </summary>
    public sealed class SwitchListEvent : IEvent
    {
        public SwitchListEvent(InteractionPointType interactionPointType)
        {
            InteractionPointType = interactionPointType;
        }

        public InteractionPointType InteractionPointType { get; }
    }

    /// <summary>
    /// Emitted after the inventory state changes.
    /// </summary>
    public sealed class InventoryUpdatedEvent : IEvent
    {
    }

}
