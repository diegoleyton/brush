using System.Threading.Tasks;

namespace Game.Core.Services
{
    /// <summary>
    /// Completes the active child's brush session against the backend.
    /// </summary>
    public interface IBrushCompletionService
    {
        Task<bool> CompleteCurrentAsync();
    }
}
