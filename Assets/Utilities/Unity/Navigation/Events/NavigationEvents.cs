using Flowbit.Utilities.Core.Events;

namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Sent when the navigation service starts a transition operation.
    /// </summary>
    public readonly struct NavigationTransitionStartedEvent : IEvent
    {
    }

    /// <summary>
    /// Sent when the navigation service finishes a transition operation.
    /// </summary>
    public readonly struct NavigationTransitionFinishedEvent : IEvent
    {
    }
}