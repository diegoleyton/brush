using System.Threading.Tasks;
using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Executes in-game market purchases against the backend for the active profile.
    /// </summary>
    public interface IMarketPurchaseService
    {
        Task<MarketPurchaseStatus> PurchaseAsync(MarketItemDefinition itemDefinition);
    }
}
