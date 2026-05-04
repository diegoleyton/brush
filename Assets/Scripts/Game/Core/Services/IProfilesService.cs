using System.Threading.Tasks;
using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Server-backed profile application service for Unity flows.
    /// </summary>
    public interface IProfilesService
    {
        bool CanUseName(string name);

        bool CanUsePetName(string name);

        Task RefreshAsync();

        Task<Profile> CreateAsync(string name, string petName, int pictureId);

        Task<bool> DeleteAsync(int profileIndex);

        void SelectProfile(int profileIndex);
    }
}
