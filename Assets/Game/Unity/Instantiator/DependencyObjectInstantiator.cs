using Flowbit.Utilities.Unity.Instantiator;

using UnityEngine;

using Zenject;

namespace Game.Unity.Instantiator
{
    /// <summary>
    /// Prefab instantiator that routes creation through Zenject so dependencies are injected.
    /// </summary>
    public sealed class DependencyObjectInstantiator : IObjectInstantiator
    {
        private readonly IInstantiator instantiator_;

        public DependencyObjectInstantiator(IInstantiator instantiator)
        {
            instantiator_ = instantiator;
        }

        public T InstantiatePrefab<T>(T prefab) where T : Component
        {
            return GetRootComponent<T>(instantiator_.InstantiatePrefab(prefab.gameObject));
        }

        public T InstantiatePrefab<T>(T prefab, Transform parent) where T : Component
        {
            GameObject instance = parent != null
                ? instantiator_.InstantiatePrefab(prefab.gameObject, parent)
                : instantiator_.InstantiatePrefab(prefab.gameObject);

            return GetRootComponent<T>(instance);
        }

        public GameObject InstantiatePrefab(GameObject prefab)
        {
            return instantiator_.InstantiatePrefab(prefab);
        }

        public GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
            return parent != null
                ? instantiator_.InstantiatePrefab(prefab, parent)
                : instantiator_.InstantiatePrefab(prefab);
        }

        private static T GetRootComponent<T>(GameObject instance) where T : Component
        {
            if (instance == null)
            {
                return null;
            }

            T component = instance.GetComponent<T>();
            if (component == null)
            {
                throw new System.InvalidOperationException(
                    $"Instantiated prefab root '{instance.name}' does not contain component {typeof(T).Name}.");
            }

            return component;
        }
    }
}
