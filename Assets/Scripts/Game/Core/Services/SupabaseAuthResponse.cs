using System;

namespace Game.Core.Services
{
    /// <summary>
    /// Minimal Supabase auth response model used by Unity auth flows.
    /// </summary>
    [Serializable]
    public sealed class SupabaseAuthResponse
    {
        public bool IsSuccess = true;
        public string ErrorMessage;
        public string access_token;
        public string refresh_token;
        public string token_type;
        public long expires_at;
        public SupabaseAuthUser user;

        public string AccessToken => access_token;
        public string RefreshToken => refresh_token;
        public string TokenType => token_type;
        public long ExpiresAtUnixSeconds => expires_at;
        public SupabaseAuthUser User => user;
    }

    [Serializable]
    public sealed class SupabaseAuthUser
    {
        public string id;
        public string email;

        public string Id => id;
        public string Email => email;
    }
}
