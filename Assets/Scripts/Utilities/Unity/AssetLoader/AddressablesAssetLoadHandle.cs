using System;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Flowbit.Utilities.Unity.AssetLoader
{
    /// <summary>
    /// Addressables-backed asset load handle adapter.
    /// </summary>
    public sealed class AddressablesAssetLoadHandle<T> : IAssetLoadHandle<T> where T : UnityEngine.Object
    {
        private readonly AsyncOperationHandle<T> operationHandle_;
        private Action<T> completed_;
        private bool released_;

        public AddressablesAssetLoadHandle(AsyncOperationHandle<T> operationHandle, Action<T> completedAction = null)
        {
            operationHandle_ = operationHandle;

            if (completedAction != null)
            {
                completed_ += completedAction;
            }

            operationHandle_.Completed += OnOperationCompleted;
        }

        public bool IsDone => operationHandle_.IsDone;
        public bool Succeeded => operationHandle_.Status == AsyncOperationStatus.Succeeded;
        public float PercentComplete => operationHandle_.PercentComplete;
        public T Asset => Succeeded ? operationHandle_.Result : null;
        public Exception OperationException => operationHandle_.OperationException;

        public event Action<T> Completed
        {
            add
            {
                if (value == null)
                {
                    return;
                }

                if (IsDone)
                {
                    value(Asset);
                    return;
                }

                completed_ += value;
            }
            remove
            {
                if (value == null)
                {
                    return;
                }

                completed_ -= value;
            }
        }

        public void Release()
        {
            if (released_)
            {
                return;
            }

            released_ = true;
            operationHandle_.Completed -= OnOperationCompleted;

            if (operationHandle_.IsValid())
            {
                Addressables.Release(operationHandle_);
            }

            completed_ = null;
        }

        private void OnOperationCompleted(AsyncOperationHandle<T> handle)
        {
            completed_?.Invoke(handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null);
        }
    }
}
