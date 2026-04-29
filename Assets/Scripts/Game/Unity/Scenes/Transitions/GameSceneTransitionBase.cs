using System.Collections;
using Flowbit.Utilities.Navigation;
using UnityEngine;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Base class for game navigation transitions.
    /// </summary>
    public abstract class GameSceneTransitionBase : MonoBehaviour, INavigationTransitionStrategy
    {
        public virtual IEnumerator PrepareTransition(NavigationTransitionContext context)
        {
            yield break;
        }

        public virtual IEnumerator FinishTransition(NavigationTransitionContext context)
        {
            yield break;
        }
    }
}
