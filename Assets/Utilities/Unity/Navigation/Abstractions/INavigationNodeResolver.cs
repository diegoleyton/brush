using UnityEngine;

namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Resolves a live navigation node from a loaded scene.
    /// </summary>
    public interface INavigationNodeResolver
    {
        /// <summary>
        /// Resolves the current active navigation node.
        /// </summary>
        INavigationNode Resolve(NavigationTarget target);
    }
}