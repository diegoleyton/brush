using System;

namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Represents a live resolved navigation node.
    /// </summary>
    public sealed class ResolvedNavigationNode
    {
        /// <summary>
        /// Creates a new resolved navigation node.
        /// </summary>
        public ResolvedNavigationNode(
            NavigationTarget target,
            INavigationNode node,
            NavigationParams navigationParams)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Node = node ?? throw new ArgumentNullException(nameof(node));
            NavigationParams = navigationParams;
        }

        /// <summary>
        /// Gets the target that created this node.
        /// </summary>
        public NavigationTarget Target { get; }

        /// <summary>
        /// Gets the live node instance.
        /// </summary>
        public INavigationNode Node { get; }

        /// <summary>
        /// Gets the parameters used to initialize the node.
        /// </summary>
        public NavigationParams NavigationParams { get; }
    }
}