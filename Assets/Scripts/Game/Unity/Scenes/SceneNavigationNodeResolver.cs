using System;
using Flowbit.Utilities.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Resolves the active navigation node from the loaded scene.
    /// </summary>
    public sealed class SceneNavigationNodeResolver : INavigationNodeResolver
    {
        public INavigationNode Resolve(NavigationTarget target)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects)
            {
                MonoBehaviour[] behaviours = rootObject.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is INavigationNode node)
                    {
                        return node;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Could not resolve an {nameof(INavigationNode)} for scene '{target?.Id ?? activeScene.name}'.");
        }
    }
}
