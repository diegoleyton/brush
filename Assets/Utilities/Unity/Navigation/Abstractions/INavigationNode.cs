namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Represents a live navigation node.
    /// </summary>
    public interface INavigationNode
    {
        /// <summary>
        /// Gets the navigation id of the node.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Initializes the node with the given parameters.
        /// </summary>
        void Initialize(NavigationParams navigationParams);
    }
}