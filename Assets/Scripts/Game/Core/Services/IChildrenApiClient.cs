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

        Task<bool> UpdateProfileAsync(string remoteProfileId, string name, string petName, int pictureId, bool isActive);

        Task<ChildGameStateSnapshot> GetGameStateAsync(string remoteProfileId);

        Task<ChildGameStateSyncPushResult> PushGameStateAsync(string remoteProfileId, string baseRevision, ChildGameStateSnapshot snapshot);

        Task<Reward[]> ClaimRewardsAsync(string remoteProfileId);

        Task<MarketPurchaseStatus> PurchaseMarketItemAsync(string remoteProfileId, MarketItemDefinition itemDefinition);

        Task<bool> GrantCoinsAsync(string remoteProfileId, int amount, string reason);
    }
}
