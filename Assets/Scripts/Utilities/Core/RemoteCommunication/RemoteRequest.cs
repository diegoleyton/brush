using System;
using System.Collections.Generic;

namespace Flowbit.Utilities.RemoteCommunication
{
    /// <summary>
    /// Immutable description of a remote request.
    /// </summary>
    public sealed class RemoteRequest
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyHeaders =
            new Dictionary<string, string>();

        public RemoteRequest(
            string url,
            RemoteRequestMethod method,
            string body = null,
            IReadOnlyDictionary<string, string> headers = null,
            string contentType = "application/json",
            int timeoutSeconds = 15,
            bool isIdempotent = false)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Remote request URL is required.", nameof(url));
            }

            Url = url.Trim();
            Method = method;
            Body = body;
            Headers = headers ?? EmptyHeaders;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/json" : contentType.Trim();
            TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 15;
            IsIdempotent = isIdempotent;
        }

        public string Url { get; }

        public RemoteRequestMethod Method { get; }

        public string Body { get; }

        public IReadOnlyDictionary<string, string> Headers { get; }

        public string ContentType { get; }

        public int TimeoutSeconds { get; }

        public bool IsIdempotent { get; }
    }
}
