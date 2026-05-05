namespace Game.Core.Services
{
    /// <summary>
    /// Result of a child game state push attempt, including sync classification.
    /// </summary>
    public sealed class ChildGameStateSyncPushResult
    {
        public ChildGameStateSyncPushStatus Status { get; set; }

        public ChildGameStateSnapshot Snapshot { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
