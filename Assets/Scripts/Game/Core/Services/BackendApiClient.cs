using System;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.RemoteCommunication;
using Game.Core.Configuration;
using UnityEngine;

namespace Game.Core.Services
{
    /// <summary>
    /// Thin Unity client for backend auth bootstrap endpoints.
    /// </summary>
    public sealed class BackendApiClient : IParentAccountApiClient
    {
        private readonly BackendSettings settings_;
        private readonly IRemoteRequestDispatcher remoteRequestDispatcher_;
        private readonly IRemotePayloadCodec payloadCodec_;
        private readonly IGameLogger logger_;

        public BackendApiClient(
            BackendSettings settings,
            IRemoteRequestDispatcher remoteRequestDispatcher,
            IRemotePayloadCodec payloadCodec,
            IGameLogger logger)
        {
            settings_ = settings ?? throw new ArgumentNullException(nameof(settings));
            remoteRequestDispatcher_ = remoteRequestDispatcher ?? throw new ArgumentNullException(nameof(remoteRequestDispatcher));
            payloadCodec_ = payloadCodec ?? throw new ArgumentNullException(nameof(payloadCodec));
            logger_ = logger;
        }

        public async Task<BackendRegisterResponse> RegisterParentAsync(string accessToken, string familyName)
        {
            string configurationError = ValidateConfiguration();
            if (!string.IsNullOrWhiteSpace(configurationError))
            {
                return new BackendRegisterResponse
                {
                    IsSuccess = false,
                    ErrorMessage = configurationError
                };
            }

            string url = $"{settings_.ApiBaseUrl}/auth/register";
            string jsonBody = "{\"familyName\":\"" + EscapeJson(familyName) + "\"}";

            RemoteRequest request = new RemoteRequest(
                url,
                RemoteRequestMethod.Post,
                body: jsonBody,
                headers: new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {accessToken}"
                },
                isIdempotent: false);

            RemoteResponse response = await remoteRequestDispatcher_.SendAsync(request);
            if (response.IsSuccess)
            {
                return payloadCodec_.Deserialize<BackendRegisterResponse>(response.Body);
            }

            string errorMessage = TryParseErrorMessage(response.Body);
            logger_?.Log($"[Auth] Register failed: {response.StatusCode} {errorMessage}");

            return new BackendRegisterResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        private string TryParseErrorMessage(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return "Request failed.";
            }

            try
            {
                ApiErrorResponse errorResponse = payloadCodec_.Deserialize<ApiErrorResponse>(payload);
                return errorResponse?.ResolveMessage() ?? payload;
            }
            catch
            {
                return payload;
            }
        }

        private static string EscapeJson(string value) =>
            (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        private string ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(settings_.ApiBaseUrl))
            {
                return "API base URL is missing in BackendSettings.json.";
            }

            return null;
        }
    }
}
