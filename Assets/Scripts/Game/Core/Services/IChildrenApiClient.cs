using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Server-side child profile client abstraction.
    /// </summary>
    public interface IChildrenApiClient
    {
        Task<IReadOnlyList<Profile>> ListAsync();

        Task<Profile> CreateAsync(string name, string petName, int pictureId);

        Task<bool> DeleteAsync(string remoteProfileId);
    }
}
