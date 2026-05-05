using System;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.Storage;

namespace Game.Core.Services
{
    /// <summary>
    /// Coordinates Supabase auth, backend bootstrap and local session persistence.
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        public const string SessionStorageKey = "marmilo_auth_session";

        private readonly IDataStorage dataStorage_;
        private readonly IRemoteIdentityProviderClient identityProviderClient_;
        private readonly IParentAccountApiClient parentAccountApiClient_;
        private readonly IGameLogger logger_;
        private Task initializeTask_;

        public AuthService(
            IDataStorage dataStorage,
            IRemoteIdentityProviderClient identityProviderClient,
            IParentAccountApiClient parentAccountApiClient,
            IGameLogger logger)
        {
            dataStorage_ = dataStorage;
            identityProviderClient_ = identityProviderClient;
            parentAccountApiClient_ = parentAccountApiClient;
            logger_ = logger;
        }

        public AuthSession CurrentSession { get; private set; }

        public bool HasSession => CurrentSession != null && CurrentSession.HasRefreshableSession;

        public async Task InitializeAsync()
        {
            if (initializeTask_ == null)
            {
                initializeTask_ = InitializeCoreAsync();
            }

            try
            {
                await initializeTask_;
            }
            catch
            {
                initializeTask_ = null;
                throw;
            }
        }

        private async Task InitializeCoreAsync()
        {
            DataLoadResult<AuthSession> loadResult =
                await dataStorage_.LoadAsync<AuthSession>(SessionStorageKey);

            CurrentSession = loadResult.Found && loadResult.Data != null && loadResult.Data.HasRefreshableSession
                ? loadResult.Data
                : null;

            if (CurrentSession != null && CurrentSession.IsExpired)
            {
                bool refreshed = await EnsureSessionIsValidAsync();
                if (!refreshed)
                {
                    return;
                }
            }

            if (CurrentSession == null && loadResult.Found && loadResult.Data != null && loadResult.Data.HasAccessToken)
            {
                await dataStorage_.SaveAsync(SessionStorageKey, new AuthSession());
                logger_?.Log("[Auth] Cleared expired auth session.");
            }

            logger_?.Log(HasSession
                ? "[Auth] Restored auth session."
                : "[Auth] No auth session found.");
        }

        public async Task<bool> EnsureSessionIsValidAsync()
        {
            if (CurrentSession == null)
            {
                return false;
            }

            if (CurrentSession.HasUsableAccessToken)
            {
                return true;
            }

            if (!CurrentSession.HasRefreshToken)
            {
                await ClearPersistedSessionAsync("[Auth] Cleared expired auth session without refresh token.");
                return false;
            }

            SupabaseAuthResponse refreshResponse =
                await identityProviderClient_.RefreshSessionAsync(CurrentSession.RefreshToken);
            if (!refreshResponse.IsSuccess)
            {
                await ClearPersistedSessionAsync("[Auth] Failed to refresh auth session.");
                return false;
            }

            AuthSession refreshedSession = ToSession(refreshResponse);
            if (refreshedSession == null)
            {
                await ClearPersistedSessionAsync("[Auth] Refresh did not return a valid auth session.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(refreshedSession.Email))
            {
                refreshedSession.Email = CurrentSession.Email;
            }

            if (string.IsNullOrWhiteSpace(refreshedSession.UserId))
            {
                refreshedSession.UserId = CurrentSession.UserId;
            }

            if (string.IsNullOrWhiteSpace(refreshedSession.RefreshToken))
            {
                refreshedSession.RefreshToken = CurrentSession.RefreshToken;
            }

            await PersistSessionAsync(refreshedSession);
            logger_?.Log("[Auth] Refreshed auth session.");
            return true;
        }

        public async Task<AuthResult> CreateAccountAsync(string email, string password, string familyName)
        {
            if (string.IsNullOrWhiteSpace(familyName))
            {
                return AuthResult.Failure("Family name is required.");
            }

            SupabaseAuthResponse signUpResponse = await identityProviderClient_.SignUpAsync(email, password);
            if (!signUpResponse.IsSuccess)
            {
                return AuthResult.Failure(signUpResponse.ErrorMessage);
            }

            AuthSession session = ToSession(signUpResponse);
            if (session == null)
            {
                return AuthResult.Failure(
                    "Supabase account was created, but no authenticated session was returned. Check email confirmation settings.");
            }

            BackendRegisterResponse registerResponse =
                await parentAccountApiClient_.RegisterParentAsync(session.AccessToken, familyName);
            if (!registerResponse.IsSuccess)
            {
                return AuthResult.Failure(registerResponse.ErrorMessage);
            }

            await PersistSessionAsync(session);
            return AuthResult.Success(session, familyRegistered: true);
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            SupabaseAuthResponse signInResponse = await identityProviderClient_.LoginAsync(email, password);
            if (!signInResponse.IsSuccess)
            {
                return AuthResult.Failure(signInResponse.ErrorMessage);
            }

            AuthSession session = ToSession(signInResponse);
            if (session == null)
            {
                return AuthResult.Failure("Supabase did not return a valid authenticated session.");
            }

            await PersistSessionAsync(session);
            return AuthResult.Success(session, familyRegistered: false);
        }

        public async Task LogoutAsync()
        {
            CurrentSession = null;
            initializeTask_ = Task.CompletedTask;
            await dataStorage_.SaveAsync(SessionStorageKey, new AuthSession());
            logger_?.Log("[Auth] Cleared auth session.");
        }

        private async Task ClearPersistedSessionAsync(string logMessage)
        {
            CurrentSession = null;
            initializeTask_ = Task.CompletedTask;
            await dataStorage_.SaveAsync(SessionStorageKey, new AuthSession());
            logger_?.Log(logMessage);
        }

        private async Task PersistSessionAsync(AuthSession session)
        {
            CurrentSession = session;
            initializeTask_ = Task.CompletedTask;
            await dataStorage_.SaveAsync(SessionStorageKey, session);
            logger_?.Log($"[Auth] Persisted session for {session.Email}.");
        }

        private static AuthSession ToSession(SupabaseAuthResponse response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.AccessToken))
            {
                return null;
            }

            return new AuthSession
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken ?? string.Empty,
                TokenType = response.TokenType ?? "bearer",
                ExpiresAtUnixSeconds = response.ExpiresAtUnixSeconds,
                UserId = response.User?.Id ?? string.Empty,
                Email = response.User?.Email ?? string.Empty
            };
        }
    }
}
