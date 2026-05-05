using System.Threading.Tasks;

using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Server-backed brush completion flow for the active profile.
    /// </summary>
    public sealed class BrushCompletionService : IBrushCompletionService
    {
        private readonly ClientGameStateStore gameStateStore_;
        private readonly IChildrenApiClient childrenApiClient_;
        private readonly IAuthService authService_;

        public BrushCompletionService(
            ClientGameStateStore gameStateStore,
            IChildrenApiClient childrenApiClient,
            IAuthService authService)
        {
            gameStateStore_ = gameStateStore;
            childrenApiClient_ = childrenApiClient;
            authService_ = authService;
        }

        public async Task<bool> CompleteCurrentAsync()
        {
            if (authService_ == null || !authService_.HasSession)
            {
                return false;
            }

            Profile currentProfile = gameStateStore_?.CurrentProfile;
            if (currentProfile == null || string.IsNullOrWhiteSpace(currentProfile.RemoteProfileId))
            {
                return false;
            }

            await childrenApiClient_.CompleteBrushSessionAsync(currentProfile.RemoteProfileId);
            return true;
        }
    }
}
