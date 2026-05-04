using System.Threading.Tasks;

namespace Game.Core.Services
{
    /// <summary>
    /// Abstracts the external identity provider used for parent authentication.
    /// </summary>
    public interface IRemoteIdentityProviderClient
    {
        Task<SupabaseAuthResponse> SignUpAsync(string email, string password);

        Task<SupabaseAuthResponse> LoginAsync(string email, string password);
    }
}
