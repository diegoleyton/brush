namespace Flowbit.Utilities.RemoteCommunication
{
    /// <summary>
    /// Raw result of a remote request.
    /// </summary>
    public sealed class RemoteResponse
    {
        public bool IsSuccess { get; set; }

        public long StatusCode { get; set; }

        public string Body { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsNetworkError { get; set; }
    }
}
