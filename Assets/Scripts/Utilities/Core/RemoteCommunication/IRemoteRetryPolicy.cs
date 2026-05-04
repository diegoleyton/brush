using System;

namespace Flowbit.Utilities.RemoteCommunication
{
    /// <summary>
    /// Decides whether a request should be retried after a failure.
    /// </summary>
    public interface IRemoteRetryPolicy
    {
        bool ShouldRetry(RemoteRequest request, RemoteResponse response, int attemptNumber);

        TimeSpan GetDelay(int attemptNumber);
    }
}
