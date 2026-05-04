using System.Threading.Tasks;
using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Remote market purchase flow for the active profile.
    /// </summary>
    public sealed class MarketPurchaseService : IMarketPurchaseService
    {
        private readonly ClientGameStateStore gameStateStore_;
        private readonly IChildrenApiClient childrenApiClient_;
        private readonly IChildGameStateSyncService childGameStateSyncService_;
        private readonly IAuthService authService_;

        public MarketPurchaseService(
            ClientGameStateStore gameStateStore,
            IChildrenApiClient childrenApiClient,
            IChildGameStateSyncService childGameStateSyncService,
            IAuthService authService)
        {
            gameStateStore_ = gameStateStore;
            childrenApiClient_ = childrenApiClient;
            childGameStateSyncService_ = childGameStateSyncService;
            authService_ = authService;
        }

        public async Task<MarketPurchaseStatus> PurchaseAsync(MarketItemDefinition itemDefinition)
        {
            if (authService_ == null || !authService_.HasSession)
            {
                return MarketPurchaseStatus.NO_CURRENT_PROFILE;
            }

            Profile currentProfile = gameStateStore_?.CurrentProfile;
            if (currentProfile == null || string.IsNullOrWhiteSpace(currentProfile.RemoteProfileId))
            {
                return MarketPurchaseStatus.NO_CURRENT_PROFILE;
            }

            if (itemDefinition == null)
            {
                return MarketPurchaseStatus.ITEM_NOT_FOUND;
            }

            MarketPurchaseStatus status = await childrenApiClient_.PurchaseMarketItemAsync(
                currentProfile.RemoteProfileId,
                itemDefinition);

            if (status == MarketPurchaseStatus.OK && childGameStateSyncService_ != null)
            {
                await childGameStateSyncService_.ReloadCurrentProfileAsync();
            }

            return status;
        }
    }
}
