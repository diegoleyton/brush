using System.Collections;

namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Plays a visual transition around a navigation operation.
    /// </summary>
    public interface INavigationTransitionStrategy
    {
        /// <summary>
        /// Plays the first half of the transition before the navigation content changes.
        /// </summary>
        IEnumerator PrepareTransition(NavigationTransitionContext context);

        /// <summary>
        /// Plays the second half of the transition after the navigation content changes.
        /// </summary>
        IEnumerator FinishTransition(NavigationTransitionContext context);
    }
}