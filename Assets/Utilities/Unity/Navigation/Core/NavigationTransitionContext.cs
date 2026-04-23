namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Contains the information required to play one stage of a navigation transition.
    /// </summary>
    public sealed class NavigationTransitionContext
    {
        /// <summary>
        /// Creates a new transition context.
        /// </summary>
        public NavigationTransitionContext(
            NavigationTarget activeTarget,
            INavigationNode activeNode,
            NavigationParams navigationParams)
        {
            ActiveTarget = activeTarget;
            ActiveNode = activeNode;
            NavigationParams = navigationParams;
        }

        /// <summary>
        /// Gets the target associated with the current transition stage.
        /// </summary>
        public NavigationTarget ActiveTarget { get; }

        /// <summary>
        /// Gets the node associated with the current transition stage.
        /// </summary>
        public INavigationNode ActiveNode { get; }

        /// <summary>
        /// Gets the navigation parameters associated with the transition.
        /// </summary>
        public NavigationParams NavigationParams { get; }
    }
}