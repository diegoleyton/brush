using System;

namespace Game.Core.Services
{
    /// <summary>
    /// Persisted authenticated parent session.
    /// </summary>
    [Serializable]
    public sealed class AuthSession
    {
        private const long ExpirationSkewSeconds = 30;

        public string AccessToken;
        public string RefreshToken;
        public string TokenType;
        public long ExpiresAtUnixSeconds;
        public string UserId;
        public string Email;

        public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);

        public bool HasRefreshToken => !string.IsNullOrWhiteSpace(RefreshToken);

        public bool IsExpired
        {
            get
            {
                if (ExpiresAtUnixSeconds <= 0)
                {
                    return false;
                }

                long nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return nowUnixSeconds >= (ExpiresAtUnixSeconds - ExpirationSkewSeconds);
            }
        }

        public bool HasUsableAccessToken => HasAccessToken && !IsExpired;

        public bool HasRefreshableSession => HasUsableAccessToken || HasRefreshToken;
    }
}
