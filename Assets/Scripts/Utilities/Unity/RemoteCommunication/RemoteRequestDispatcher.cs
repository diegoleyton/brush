using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.RemoteCommunication;
using UnityEngine.Networking;

namespace Flowbit.Utilities.Unity.RemoteCommunication
{
    /// <summary>
    /// Unity transport for remote requests with bounded concurrency and retries.
    /// </summary>
    public sealed class RemoteRequestDispatcher : IRemoteRequestDispatcher
    {
        private readonly IRemoteRetryPolicy retryPolicy_;
        private readonly IGameLogger logger_;
        private readonly SemaphoreSlim concurrencyGate_;
        private readonly DevelopmentRemoteSimulationSettings simulationSettings_;

        public RemoteRequestDispatcher(
            IRemoteRetryPolicy retryPolicy,
            IGameLogger logger,
            DevelopmentRemoteSimulationSettings simulationSettings,
            int maxConcurrentRequests = 4)
        {
            retryPolicy_ = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            logger_ = logger;
            simulationSettings_ = simulationSettings;
            concurrencyGate_ = new SemaphoreSlim(Math.Max(1, maxConcurrentRequests), Math.Max(1, maxConcurrentRequests));
        }

        public async Task<RemoteResponse> SendAsync(RemoteRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await concurrencyGate_.WaitAsync();

            try
            {
                int attemptNumber = 1;
                while (true)
                {
                    RemoteResponse response = await SendOnceAsync(request);
                    if (!retryPolicy_.ShouldRetry(request, response, attemptNumber))
                    {
                        return response;
                    }

                    TimeSpan delay = retryPolicy_.GetDelay(attemptNumber);
                    logger_?.Log(
                        $"[Remote] Retrying {request.Method} {request.Url} in {delay.TotalMilliseconds:0} ms (attempt {attemptNumber + 1}).");
                    await Task.Delay(delay);
                    attemptNumber++;
                }
            }
            finally
            {
                concurrencyGate_.Release();
            }
        }

        private async Task<RemoteResponse> SendOnceAsync(RemoteRequest request)
        {
            if (simulationSettings_?.SimulateNetworkFailure == true)
            {
                const string simulatedNetworkFailureError = "Simulated network failure.";
                logger_?.Log($"[Remote] Simulated network failure for {request.Method} {request.Url}.");
                return new RemoteResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Body = string.Empty,
                    ErrorMessage = simulatedNetworkFailureError,
                    IsNetworkError = true
                };
            }

            if (simulationSettings_?.SimulateUnresponsiveNetwork == true)
            {
                double timeoutSeconds = Math.Max(1, request.TimeoutSeconds);
                double extraDelaySeconds = Math.Max(0f, simulationSettings_.UnresponsiveNetworkExtraDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds + extraDelaySeconds));

                string simulatedTimeoutError = "Simulated network timeout.";
                logger_?.Log($"[Remote] Simulated unresponsive network for {request.Method} {request.Url}.");
                return new RemoteResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Body = string.Empty,
                    ErrorMessage = simulatedTimeoutError,
                    IsNetworkError = true
                };
            }

            if (simulationSettings_?.SimulateSlowNetwork == true)
            {
                double delaySeconds = Math.Max(0f, simulationSettings_.SlowNetworkDelaySeconds);
                if (delaySeconds > 0f)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }

            using UnityWebRequest unityWebRequest = new UnityWebRequest(request.Url, ToUnityMethod(request.Method));
            unityWebRequest.timeout = request.TimeoutSeconds;
            unityWebRequest.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(request.Body))
            {
                byte[] body = Encoding.UTF8.GetBytes(request.Body);
                unityWebRequest.uploadHandler = new UploadHandlerRaw(body);
                unityWebRequest.SetRequestHeader("Content-Type", request.ContentType);
            }

            foreach (var header in request.Headers)
            {
                unityWebRequest.SetRequestHeader(header.Key, header.Value);
            }

            await SendUnityRequestAsync(unityWebRequest);

            bool networkError =
                unityWebRequest.result == UnityWebRequest.Result.ConnectionError ||
                unityWebRequest.result == UnityWebRequest.Result.DataProcessingError;

            bool success =
                unityWebRequest.result == UnityWebRequest.Result.Success &&
                unityWebRequest.responseCode >= 200 &&
                unityWebRequest.responseCode <= 299;

            string bodyText = unityWebRequest.downloadHandler != null
                ? unityWebRequest.downloadHandler.text
                : string.Empty;

            string errorMessage = success
                ? null
                : !string.IsNullOrWhiteSpace(bodyText)
                    ? bodyText
                    : unityWebRequest.error;

            if (!success)
            {
                logger_?.Log(
                    $"[Remote] Request failed {request.Method} {request.Url} ({unityWebRequest.responseCode}): {errorMessage}");
            }

            return new RemoteResponse
            {
                IsSuccess = success,
                StatusCode = unityWebRequest.responseCode,
                Body = bodyText,
                ErrorMessage = errorMessage,
                IsNetworkError = networkError
            };
        }

        private static Task SendUnityRequestAsync(UnityWebRequest request)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ => tcs.TrySetResult(true);
            return tcs.Task;
        }

        private static string ToUnityMethod(RemoteRequestMethod method)
        {
            switch (method)
            {
                case RemoteRequestMethod.Get:
                    return UnityWebRequest.kHttpVerbGET;
                case RemoteRequestMethod.Post:
                    return UnityWebRequest.kHttpVerbPOST;
                case RemoteRequestMethod.Put:
                    return UnityWebRequest.kHttpVerbPUT;
                case RemoteRequestMethod.Patch:
                    return "PATCH";
                case RemoteRequestMethod.Delete:
                    return UnityWebRequest.kHttpVerbDELETE;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }
        }
    }
}
