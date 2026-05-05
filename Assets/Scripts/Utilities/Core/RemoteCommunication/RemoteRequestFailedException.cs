using System;

namespace Flowbit.Utilities.RemoteCommunication
{
    /// <summary>
    /// Typed exception for remote operations so presentation can distinguish transport failures from server rejections.
    /// </summary>
    public sealed class RemoteRequestFailedException : Exception
    {
        public RemoteRequestFailedException(
            string message,
            bool isNetworkError,
            long statusCode = 0,
            Exception innerException = null)
            : base(message, innerException)
        {
            IsNetworkError = isNetworkError;
            StatusCode = statusCode;
        }

        public bool IsNetworkError { get; }

        public long StatusCode { get; }
    }
}
