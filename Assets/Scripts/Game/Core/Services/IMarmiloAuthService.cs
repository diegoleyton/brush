using System.Threading.Tasks;

namespace Game.Core.Services
{
    /// <summary>
    /// Handles parent authentication and session persistence.
    /// </summary>
    public interface IMarmiloAuthService
    {
        MarmiloAuthSession CurrentSession { get; }

        bool HasSession { get; }

        Task InitializeAsync();

        Task<MarmiloAuthResult> CreateAccountAsync(string email, string password, string familyName);

        Task<MarmiloAuthResult> LoginAsync(string email, string password);

        Task LogoutAsync();
    }
}
