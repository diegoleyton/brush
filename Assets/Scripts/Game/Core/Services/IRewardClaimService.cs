using System.Threading.Tasks;
using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Claims the active child's pending rewards from the backend and refreshes local state.
    /// </summary>
    public interface IRewardClaimService
    {
        Task<Reward[]> ClaimAsync();
    }
}
