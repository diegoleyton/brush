using System;

namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Represents a scene navigation entry stored in the history.
    /// </summary>
    public sealed class NavigationHistoryEntry
    {
        /// <summary>
        /// Creates a new history entry.
        /// </summary>
        public NavigationHistoryEntry(
            NavigationTarget target,
            NavigationParams navigationParams)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            NavigationParams = navigationParams;
        }

        /// <summary>
        /// Gets the target.
        /// </summary>
        public NavigationTarget Target { get; }

        /// <summary>
        /// Gets the navigation parameters.
        /// </summary>
        public NavigationParams NavigationParams { get; }
    }
}