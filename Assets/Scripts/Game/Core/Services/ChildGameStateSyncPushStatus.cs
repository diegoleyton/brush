namespace Game.Core.Services
{
    /// <summary>
    /// Describes the outcome of pushing a child game state snapshot to the backend.
    /// </summary>
    public enum ChildGameStateSyncPushStatus
    {
        Success,
        TransportFailure,
        Conflict,
        ServerRejected
    }
}
