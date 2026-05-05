using Flowbit.Utilities.Core.Events;

namespace Game.Core.Events
{
    /// <summary>
    /// Emitted when a protected backend request reports that the current auth session is no longer valid.
    /// </summary>
    public sealed class AuthSessionInvalidatedEvent : IEvent
    {
        public AuthSessionInvalidatedEvent(string message)
        {
            Message = message ?? "Your session is no longer valid. Please log in again.";
        }

        public string Message { get; }
    }
}
