using System;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.RemoteCommunication;
using Game.Core.Configuration;
using UnityEngine;

namespace Game.Core.Services
{
    /// <summary>
    /// Thin Unity client for Supabase email/password auth endpoints.
    /// </summary>
    public sealed class SupabaseAuthApiClient : IRemoteIdentityProviderClient
    {
        private readonly BackendSettings settings_;
        private readonly IRemoteRequestDispatcher remoteRequestDispatcher_;
        private readonly IRemotePayloadCodec payloadCodec_;
        private readonly IGameLogger logger_;

        public SupabaseAuthApiClient(
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

        public Task<SupabaseAuthResponse> SignUpAsync(string email, string password) =>
            SendAuthRequestAsync(
                $"{settings_.SupabaseUrl}/auth/v1/signup",
                "{\"email\":\"" + EscapeJson(email) + "\",\"password\":\"" + EscapeJson(password) + "\"}");

        public Task<SupabaseAuthResponse> LoginAsync(string email, string password) =>
            SendAuthRequestAsync(
                $"{settings_.SupabaseUrl}/auth/v1/token?grant_type=password",
                "{\"email\":\"" + EscapeJson(email) + "\",\"password\":\"" + EscapeJson(password) + "\"}");

        public Task<SupabaseAuthResponse> RefreshSessionAsync(string refreshToken) =>
            SendAuthRequestAsync(
                $"{settings_.SupabaseUrl}/auth/v1/token?grant_type=refresh_token",
                "{\"refresh_token\":\"" + EscapeJson(refreshToken) + "\"}");

        private async Task<SupabaseAuthResponse> SendAuthRequestAsync(string url, string jsonBody)
        {
            string configurationError = ValidateConfiguration();
            if (!string.IsNullOrWhiteSpace(configurationError))
            {
                return new SupabaseAuthResponse
                {
                    IsSuccess = false,
                    ErrorMessage = configurationError
                };
            }

            RemoteRequest request = new RemoteRequest(
                url,
                RemoteRequestMethod.Post,
                body: jsonBody,
                headers: new System.Collections.Generic.Dictionary<string, string>
                {
                    ["apikey"] = settings_.SupabaseAnonKey
                },
                isIdempotent: false);

            RemoteResponse response = await remoteRequestDispatcher_.SendAsync(request);
            if (response.IsSuccess)
            {
                return payloadCodec_.Deserialize<SupabaseAuthResponse>(response.Body);
            }

            string errorMessage = TryParseErrorMessage(response.Body);
            logger_?.Log($"[Auth] Supabase request failed: {response.StatusCode} {errorMessage}");

            return new SupabaseAuthResponse
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
            if (string.IsNullOrWhiteSpace(settings_.SupabaseUrl))
            {
                return "Supabase URL is missing in BackendSettings.json.";
            }

            if (string.IsNullOrWhiteSpace(settings_.SupabaseAnonKey))
            {
                return "Supabase anon key is missing in BackendSettings.json.";
            }

            return null;
        }
    }
}
