namespace Flowbit.Utilities.ScreenBlocker
{
    /// <summary>
    /// Describes a screen block request and its optional loading presentation.
    /// </summary>
    public sealed class ScreenBlockerRequest
    {
        public string Reason { get; init; } = string.Empty;

        public bool ShowLoadingWithTime { get; init; }

        public string LoadingMessage { get; init; }
    }
}
