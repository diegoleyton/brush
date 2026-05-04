using System;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.Storage;
using Game.Unity.Settings;

namespace Game.Core.Services
{
    /// <summary>
    /// Coordinates Supabase auth, Marmilo backend bootstrap and local session persistence.
    /// </summary>
    public sealed class MarmiloAuthService : IMarmiloAuthService
    {
        public const string SessionStorageKey = "marmilo_auth_session";

        private readonly IDataStorage dataStorage_;
        private readonly IRemoteIdentityProviderClient identityProviderClient_;
        private readonly IParentAccountApiClient parentAccountApiClient_;
        private readonly IGameLogger logger_;

        public MarmiloAuthService(
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

        public MarmiloAuthSession CurrentSession { get; private set; }

        public bool HasSession => CurrentSession != null && CurrentSession.HasAccessToken;

        public async Task InitializeAsync()
        {
            DataLoadResult<MarmiloAuthSession> loadResult =
                await dataStorage_.LoadAsync<MarmiloAuthSession>(SessionStorageKey);

            CurrentSession = loadResult.Success && loadResult.Data != null && loadResult.Data.HasAccessToken
                ? loadResult.Data
                : null;

            logger_?.Log(HasSession
                ? "[Auth] Restored Marmilo auth session."
                : "[Auth] No Marmilo auth session found.");
        }

        public async Task<MarmiloAuthResult> CreateAccountAsync(string email, string password, string familyName)
        {
            if (string.IsNullOrWhiteSpace(familyName))
            {
                return MarmiloAuthResult.Failure("Family name is required.");
            }

            SupabaseAuthResponse signUpResponse = await identityProviderClient_.SignUpAsync(email, password);
            if (!signUpResponse.IsSuccess)
            {
                return MarmiloAuthResult.Failure(signUpResponse.ErrorMessage);
            }

            MarmiloAuthSession session = ToSession(signUpResponse);
            if (session == null)
            {
                return MarmiloAuthResult.Failure(
                    "Supabase account was created, but no authenticated session was returned. Check email confirmation settings.");
            }

            MarmiloBackendRegisterResponse registerResponse =
                await parentAccountApiClient_.RegisterParentAsync(session.AccessToken, familyName);
            if (!registerResponse.IsSuccess)
            {
                return MarmiloAuthResult.Failure(registerResponse.ErrorMessage);
            }

            await PersistSessionAsync(session);
            return MarmiloAuthResult.Success(session, familyRegistered: true);
        }

        public async Task<MarmiloAuthResult> LoginAsync(string email, string password)
        {
            SupabaseAuthResponse signInResponse = await identityProviderClient_.LoginAsync(email, password);
            if (!signInResponse.IsSuccess)
            {
                return MarmiloAuthResult.Failure(signInResponse.ErrorMessage);
            }

            MarmiloAuthSession session = ToSession(signInResponse);
            if (session == null)
            {
                return MarmiloAuthResult.Failure("Supabase did not return a valid authenticated session.");
            }

            await PersistSessionAsync(session);
            return MarmiloAuthResult.Success(session, familyRegistered: false);
        }

        public async Task LogoutAsync()
        {
            CurrentSession = null;
            await dataStorage_.SaveAsync(SessionStorageKey, new MarmiloAuthSession());
            logger_?.Log("[Auth] Cleared Marmilo auth session.");
        }

        private async Task PersistSessionAsync(MarmiloAuthSession session)
        {
            CurrentSession = session;
            await dataStorage_.SaveAsync(SessionStorageKey, session);
            logger_?.Log($"[Auth] Persisted session for {session.Email}.");
        }

        private static MarmiloAuthSession ToSession(SupabaseAuthResponse response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.AccessToken))
            {
                return null;
            }

            return new MarmiloAuthSession
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
