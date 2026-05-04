using System;

namespace Game.Core.Services
{
    /// <summary>
    /// Persisted authenticated parent session.
    /// </summary>
    [Serializable]
    public sealed class AuthSession
    {
        public string AccessToken;
        public string RefreshToken;
        public string TokenType;
        public long ExpiresAtUnixSeconds;
        public string UserId;
        public string Email;

        public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);
    }
}
