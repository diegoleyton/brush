using UnityEngine;

namespace Flowbit.Utilities.Unity.Instantiator
{
    /// <summary>
    /// Pure Unity prefab instantiator without dependency injection.
    /// </summary>
    public sealed class UnityInstantiator : IObjectInstantiator
    {
        public T InstantiatePrefab<T>(T prefab) where T : Component
        {
            return Object.Instantiate(prefab);
        }

        public T InstantiatePrefab<T>(T prefab, Transform parent) where T : Component
        {
            return Object.Instantiate(prefab, parent, false);
        }

        public GameObject InstantiatePrefab(GameObject prefab)
        {
            return Object.Instantiate(prefab);
        }

        public GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
            return Object.Instantiate(prefab, parent, false);
        }
    }
}
