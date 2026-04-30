using System.Collections;

namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Provides navigation between scene and prefab nodes.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Gets whether the navigator can go back to a previous scene.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// Sets the initial current node without performing a navigation transition.
        /// </summary>
        void StartWith(NavigationTarget target, NavigationParams navigationParams = null);

        /// <summary>
        /// Navigates to the given target.
        /// </summary>
        IEnumerator Navigate(NavigationTarget target, NavigationParams navigationParams = null);

        /// <summary>
        /// Navigates to the given target without adding the current scene to navigation history.
        /// </summary>
        IEnumerator NavigateWithoutHistory(NavigationTarget target, NavigationParams navigationParams = null);

        /// <summary>
        /// Navigates back to the previous scene in the history.
        /// </summary>
        IEnumerator Back();

        /// <summary>
        /// Closes the opened prefab with the given id.
        /// </summary>
        IEnumerator Close(string id);
    }
}
