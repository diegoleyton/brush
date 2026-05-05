using Flowbit.Utilities.Core.Events;

namespace Game.Core.Events
{
    /// <summary>
    /// Emitted when optimistic child game state sync status changes.
    /// </summary>
    public sealed class ChildGameStateSyncStatusChangedEvent : IEvent
    {
        public ChildGameStateSyncStatusChangedEvent(bool hasPendingSync, bool isOffline)
        {
            HasPendingSync = hasPendingSync;
            IsOffline = isOffline;
        }

        public bool HasPendingSync { get; }

        public bool IsOffline { get; }
    }

    /// <summary>
    /// Emitted when a non-blocking child game state sync fails with a server-side rejection.
    /// </summary>
    public sealed class ChildGameStateSyncFailureEvent : IEvent
    {
        public ChildGameStateSyncFailureEvent(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
