namespace Game.Core.Services
{
    /// <summary>
    /// Result of login or account creation flows.
    /// </summary>
    public sealed class AuthResult
    {
        public static AuthResult Success(AuthSession session, bool familyRegistered) =>
            new AuthResult
            {
                IsSuccess = true,
                Session = session,
                FamilyRegistered = familyRegistered
            };

        public static AuthResult Failure(string errorMessage) =>
            new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage ?? "Unknown authentication error."
            };

        public bool IsSuccess { get; private set; }

        public bool FamilyRegistered { get; private set; }

        public string ErrorMessage { get; private set; } = string.Empty;

        public AuthSession Session { get; private set; }
    }
}
