using System;

using UnityEngine;

namespace Flowbit.Utilities.Unity.AssetLoader
{
    /// <summary>
    /// Represents a framework-agnostic asynchronous asset load operation.
    /// </summary>
    public interface IAssetLoadHandle<out T> where T : UnityEngine.Object
    {
        bool IsDone { get; }
        bool Succeeded { get; }
        float PercentComplete { get; }
        T Asset { get; }
        Exception OperationException { get; }

        event Action<T> Completed;

        void Release();
    }
}
