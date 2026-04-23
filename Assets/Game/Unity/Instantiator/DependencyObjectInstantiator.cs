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
            return instantiator_.InstantiatePrefabForComponent<T>(prefab);
        }

        public T InstantiatePrefab<T>(T prefab, Transform parent) where T : Component
        {
            return parent != null
                ? instantiator_.InstantiatePrefabForComponent<T>(prefab, parent)
                : instantiator_.InstantiatePrefabForComponent<T>(prefab);
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
    }
}
