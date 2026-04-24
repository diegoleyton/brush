using UnityEngine;

namespace Flowbit.Utilities.Unity.Instantiator
{
    /// <summary>
    /// Abstraction over prefab instantiation for Unity objects and components.
    /// </summary>
    public interface IObjectInstantiator
    {
        T InstantiatePrefab<T>(T prefab) where T : Component;
        T InstantiatePrefab<T>(T prefab, Transform parent) where T : Component;
        GameObject InstantiatePrefab(GameObject prefab);
        GameObject InstantiatePrefab(GameObject prefab, Transform parent);
    }
}
