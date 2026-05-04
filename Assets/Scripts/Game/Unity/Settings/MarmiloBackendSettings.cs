using System;
using UnityEngine;

namespace Game.Unity.Settings
{
    /// <summary>
    /// Runtime settings for Supabase and Marmilo backend endpoints.
    /// </summary>
    [Serializable]
    public sealed class MarmiloBackendSettings
    {
        public const string ResourceFileName = "MarmiloBackendSettings";

        [Serializable]
        private sealed class MarmiloBackendSettingsPayload
        {
            public string ApiBaseUrl = "http://localhost:5027";
            public string SupabaseUrl = string.Empty;
            public string SupabaseAnonKey = string.Empty;
        }

        public string ApiBaseUrl { get; private set; } = "http://localhost:5027";

        public string SupabaseUrl { get; private set; } = string.Empty;

        public string SupabaseAnonKey { get; private set; } = string.Empty;

        public static MarmiloBackendSettings FromJson(string json)
        {
            MarmiloBackendSettingsPayload payload = string.IsNullOrWhiteSpace(json)
                ? new MarmiloBackendSettingsPayload()
                : JsonUtility.FromJson<MarmiloBackendSettingsPayload>(json) ?? new MarmiloBackendSettingsPayload();

            return new MarmiloBackendSettings
            {
                ApiBaseUrl = NormalizeUrl(payload.ApiBaseUrl, "http://localhost:5027"),
                SupabaseUrl = NormalizeUrl(payload.SupabaseUrl, string.Empty),
                SupabaseAnonKey = payload.SupabaseAnonKey?.Trim() ?? string.Empty
            };
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                throw new InvalidOperationException("Marmilo API base URL is required.");
            }

            if (string.IsNullOrWhiteSpace(SupabaseUrl))
            {
                throw new InvalidOperationException("Supabase URL is required.");
            }

            if (string.IsNullOrWhiteSpace(SupabaseAnonKey))
            {
                throw new InvalidOperationException("Supabase anon key is required.");
            }
        }

        private static string NormalizeUrl(string value, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return normalized.TrimEnd('/');
        }
    }
}
