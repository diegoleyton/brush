using Flowbit.Utilities.Core.Events;

namespace Game.Core.Events
{
    /// <summary>
    /// Emitted after persisted data has been loaded into memory.
    /// </summary>
    public sealed class DataLoadedEvent : IEvent
    {
    }

    /// <summary>
    /// Emitted after the application bootstrap has completed.
    /// </summary>
    public sealed class AppReadyEvent : IEvent
    {
    }
}
