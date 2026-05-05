using Flowbit.Utilities.Core.Events;

using Game.Core.Data;

namespace Game.Core.Events
{
    /// <summary>
    /// Emitted after a save-related operation completes.
    /// </summary>
    public sealed class DataSavedEvent : IEvent
    {
        public DataSavedEvent(DataSaveStatus status)
        {
            Status = status;
        }

        public DataSaveStatus Status { get; }
    }

    /// <summary>
    /// Emitted after local game data changes and should be persisted.
    /// </summary>
    public sealed class LocalDataChangedEvent : IEvent
    {
    }

    /// <summary>
    /// Emitted after locally optimistic child game state changes that should be synchronized remotely.
    /// </summary>
    public sealed class ChildGameStateLocallyChangedEvent : IEvent
    {
    }
}
