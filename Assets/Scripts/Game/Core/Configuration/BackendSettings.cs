using System;

namespace Game.Core.Configuration
{
    /// <summary>
    /// Runtime settings for Supabase and backend endpoints.
    /// </summary>
    public sealed class BackendSettings
    {
        public const string ResourceFileName = "BackendSettings";

        public BackendSettings(
            string apiBaseUrl = "http://localhost:5027",
            string supabaseUrl = "",
            string supabaseAnonKey = "")
        {
            ApiBaseUrl = NormalizeUrl(apiBaseUrl, "http://localhost:5027");
            SupabaseUrl = NormalizeUrl(supabaseUrl, string.Empty);
            SupabaseAnonKey = supabaseAnonKey?.Trim() ?? string.Empty;
        }

        public string ApiBaseUrl { get; }

        public string SupabaseUrl { get; }

        public string SupabaseAnonKey { get; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
            {
                throw new InvalidOperationException("API base URL is required.");
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
