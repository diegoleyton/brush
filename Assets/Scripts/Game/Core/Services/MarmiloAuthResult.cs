namespace Game.Core.Services
{
    /// <summary>
    /// Result of login or account creation flows.
    /// </summary>
    public sealed class MarmiloAuthResult
    {
        public static MarmiloAuthResult Success(MarmiloAuthSession session, bool familyRegistered) =>
            new MarmiloAuthResult
            {
                IsSuccess = true,
                Session = session,
                FamilyRegistered = familyRegistered
            };

        public static MarmiloAuthResult Failure(string errorMessage) =>
            new MarmiloAuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage ?? "Unknown authentication error."
            };

        public bool IsSuccess { get; private set; }

        public bool FamilyRegistered { get; private set; }

        public string ErrorMessage { get; private set; } = string.Empty;

        public MarmiloAuthSession Session { get; private set; }
    }
}
