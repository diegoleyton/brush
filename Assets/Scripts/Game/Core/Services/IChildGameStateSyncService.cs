using System.Threading.Tasks;

namespace Game.Core.Services
{
    /// <summary>
    /// Coordinates remote load/save of the active child's game state.
    /// </summary>
    public interface IChildGameStateSyncService
    {
        Task EnsureCurrentProfileLoadedAsync();
    }
}
