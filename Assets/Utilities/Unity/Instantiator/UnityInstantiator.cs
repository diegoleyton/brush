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
            return GetRootComponent<T>(Object.Instantiate(prefab.gameObject));
        }

        public T InstantiatePrefab<T>(T prefab, Transform parent) where T : Component
        {
            return GetRootComponent<T>(Object.Instantiate(prefab.gameObject, parent, false));
        }

        public GameObject InstantiatePrefab(GameObject prefab)
        {
            return Object.Instantiate(prefab);
        }

        public GameObject InstantiatePrefab(GameObject prefab, Transform parent)
        {
            return Object.Instantiate(prefab, parent, false);
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
