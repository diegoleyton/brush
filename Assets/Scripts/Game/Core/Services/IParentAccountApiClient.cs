using System.Threading.Tasks;

namespace Game.Core.Services
{
    /// <summary>
    /// Abstracts the Marmilo account bootstrap API used after authentication succeeds.
    /// </summary>
    public interface IParentAccountApiClient
    {
        Task<BackendRegisterResponse> RegisterParentAsync(string accessToken, string familyName);
    }
}
