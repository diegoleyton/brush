namespace Flowbit.Utilities.ScreenBlocker
{
    /// <summary>
    /// Describes a screen block request and its optional loading presentation.
    /// </summary>
    public sealed class ScreenBlockerRequest
    {
        public string Reason { get; set; } = string.Empty;

        public bool ShowLoadingWithTime { get; set; }

        public string LoadingMessage { get; set; }

        public bool ShowLoadingImmediately { get; set; }
    }
}
