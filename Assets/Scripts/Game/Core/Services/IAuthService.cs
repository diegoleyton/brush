using System.Threading.Tasks;

namespace Game.Core.Services
{
    /// <summary>
    /// Handles parent authentication and session persistence.
    /// </summary>
    public interface IAuthService
    {
        AuthSession CurrentSession { get; }

        bool HasSession { get; }

        Task InitializeAsync();

        Task<AuthResult> CreateAccountAsync(string email, string password, string familyName);

        Task<AuthResult> LoginAsync(string email, string password);

        Task LogoutAsync();
    }
}
