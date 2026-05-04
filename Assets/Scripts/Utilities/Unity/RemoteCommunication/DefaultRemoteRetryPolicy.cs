using System;
using Flowbit.Utilities.RemoteCommunication;

namespace Flowbit.Utilities.Unity.RemoteCommunication
{
    /// <summary>
    /// Simple retry policy for transient failures on idempotent requests.
    /// </summary>
    public sealed class DefaultRemoteRetryPolicy : IRemoteRetryPolicy
    {
        private readonly int maxAttempts_;

        public DefaultRemoteRetryPolicy(int maxAttempts = 2)
        {
            maxAttempts_ = Math.Max(1, maxAttempts);
        }

        public bool ShouldRetry(RemoteRequest request, RemoteResponse response, int attemptNumber)
        {
            if (request == null || response == null)
            {
                return false;
            }

            if (attemptNumber >= maxAttempts_ || !request.IsIdempotent)
            {
                return false;
            }

            if (response.IsNetworkError)
            {
                return true;
            }

            return response.StatusCode == 408 ||
                   response.StatusCode == 429 ||
                   response.StatusCode == 500 ||
                   response.StatusCode == 502 ||
                   response.StatusCode == 503 ||
                   response.StatusCode == 504;
        }

        public TimeSpan GetDelay(int attemptNumber)
        {
            return TimeSpan.FromMilliseconds(250 * Math.Max(1, attemptNumber));
        }
    }
}
