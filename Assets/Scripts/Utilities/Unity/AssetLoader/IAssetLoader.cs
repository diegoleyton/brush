using System;

using UnityEngine;

namespace Flowbit.Utilities.Unity.AssetLoader
{
    /// <summary>
    /// Abstraction over asynchronous asset loading strategies.
    /// </summary>
    public interface IAssetLoader
    {
        IAssetLoadHandle<T> LoadAssetAsync<T>(string key, Action<T> completedAction = null)
            where T : UnityEngine.Object;
    }
}
