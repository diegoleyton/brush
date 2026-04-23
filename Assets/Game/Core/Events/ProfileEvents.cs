using Flowbit.Utilities.Core.Events;

namespace Game.Core.Events
{
    /// <summary>
    /// Emitted after the current profile changes.
    /// </summary>
    public sealed class ProfileSwitchedEvent : IEvent
    {
    }

    /// <summary>
    /// Emitted after the current profile data changes.
    /// </summary>
    public sealed class ProfileUpdatedEvent : IEvent
    {
    }

    /// <summary>
    /// Emitted when a profile has a pending reward to claim.
    /// </summary>
    public sealed class PendingRewardEvent : IEvent
    {
    }
}
