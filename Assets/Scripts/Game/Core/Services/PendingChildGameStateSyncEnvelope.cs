using System;
using System.Collections.Generic;

namespace Game.Core.Services
{
    /// <summary>
    /// Persisted local queue of child game state snapshots waiting to be synchronized.
    /// </summary>
    [Serializable]
    public sealed class PendingChildGameStateSyncEnvelope
    {
        public List<PendingChildGameStateSyncEntry> Entries = new List<PendingChildGameStateSyncEntry>();
    }

    /// <summary>
    /// Persisted optimistic local change for one remote child game state.
    /// </summary>
    [Serializable]
    public sealed class PendingChildGameStateSyncEntry
    {
        public string RemoteProfileId;
        public string BaseRevision;
        public ChildGameStateSnapshot Snapshot = new ChildGameStateSnapshot();
    }
}
