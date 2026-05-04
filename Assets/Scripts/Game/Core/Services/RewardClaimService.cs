using System.Threading.Tasks;
using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Remote reward claim flow for the active profile.
    /// </summary>
    public sealed class RewardClaimService : IRewardClaimService
    {
        private readonly ClientGameStateStore gameStateStore_;
        private readonly IChildrenApiClient childrenApiClient_;
        private readonly IChildGameStateSyncService childGameStateSyncService_;
        private readonly IAuthService authService_;

        public RewardClaimService(
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

        public async Task<Reward[]> ClaimAsync()
        {
            if (authService_ == null || !authService_.HasSession)
            {
                return null;
            }

            Profile currentProfile = gameStateStore_?.CurrentProfile;
            if (currentProfile == null || string.IsNullOrWhiteSpace(currentProfile.RemoteProfileId))
            {
                return null;
            }

            Reward[] rewards = await childrenApiClient_.ClaimRewardsAsync(currentProfile.RemoteProfileId);
            if (rewards == null)
            {
                return null;
            }

            if (childGameStateSyncService_ != null)
            {
                await childGameStateSyncService_.ReloadCurrentProfileAsync();
            }

            return rewards;
        }
    }
}
