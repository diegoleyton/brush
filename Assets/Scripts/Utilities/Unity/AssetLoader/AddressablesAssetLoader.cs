using System;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Flowbit.Utilities.Unity.AssetLoader
{
    /// <summary>
    /// Default asset loader implementation backed by Unity Addressables.
    /// </summary>
    public sealed class AddressablesAssetLoader : IAssetLoader
    {
        public IAssetLoadHandle<T> LoadAssetAsync<T>(string key, Action<T> completedAction = null)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Asset key cannot be null or empty.", nameof(key));
            }

            AsyncOperationHandle<T> operationHandle = Addressables.LoadAssetAsync<T>(key);
            return new AddressablesAssetLoadHandle<T>(operationHandle, completedAction);
        }
    }
}
