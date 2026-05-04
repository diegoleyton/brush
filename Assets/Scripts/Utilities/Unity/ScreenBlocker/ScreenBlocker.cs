using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flowbit.Utilities.ScreenBlocker
{
    /// <summary>
    /// Blocks user input using a full-screen image, supporting multiple concurrent locks.
    /// </summary>
    public class ScreenBlocker
    {
        private sealed class BlockState
        {
            public string Reason;
            public bool ShowLoadingWithTime;
            public string LoadingMessage;
            public float StartedAtRealtime;
        }

        private readonly ScreenBlockerView blockerImage_;
        private readonly Dictionary<int, BlockState> activeLocks_ = new();
        private int idCounter_;

        public bool IsBlocked => activeLocks_.Count > 0;

        public ScreenBlocker(ScreenBlockerView blockerImage)
        {
            blockerImage_ = blockerImage;

            if (blockerImage_?.Image != null)
            {
                blockerImage_.Image.raycastTarget = true;
                blockerImage_.Image.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Blocks the screen and returns a disposable scope that will unblock automatically.
        /// </summary>
        public IDisposable BlockScope(
            string reason = "",
            bool showLoadingWithTime = false,
            string loadingMessage = null)
        {
            return BlockScope(new ScreenBlockerRequest
            {
                Reason = reason,
                ShowLoadingWithTime = showLoadingWithTime,
                LoadingMessage = loadingMessage
            });
        }

        public IDisposable BlockScope(ScreenBlockerRequest request)
        {
            int id = Block(request ?? new ScreenBlockerRequest());
            return new Scope(this, id);
        }

        /// <summary>
        /// Clears all locks (failsafe).
        /// </summary>
        public void ClearAll()
        {
            activeLocks_.Clear();
            UpdateBlocker();
        }

        private int Block(ScreenBlockerRequest request)
        {
            int id = ++idCounter_;
            activeLocks_[id] = new BlockState
            {
                Reason = request?.Reason,
                ShowLoadingWithTime = request?.ShowLoadingWithTime ?? false,
                LoadingMessage = request?.LoadingMessage,
                StartedAtRealtime = Time.realtimeSinceStartup
            };

            UpdateBlocker();
            return id;
        }

        private void Unblock(int id)
        {
            if (!activeLocks_.Remove(id))
            {
                Debug.LogWarning($"ScreenBlocker: tried to unblock invalid id {id}");
                return;
            }

            UpdateBlocker();
        }

        private void UpdateBlocker()
        {
            if (blockerImage_?.Image != null)
            {
                blockerImage_.Image.gameObject.SetActive(activeLocks_.Count > 0);
            }

            if (blockerImage_ == null)
            {
                return;
            }

            if (activeLocks_.Count == 0)
            {
                blockerImage_.HideLoading();
                return;
            }

            int latestLockId = -1;
            BlockState latestLoadingState = null;
            foreach (KeyValuePair<int, BlockState> entry in activeLocks_)
            {
                if (!entry.Value.ShowLoadingWithTime)
                {
                    continue;
                }

                if (entry.Key > latestLockId)
                {
                    latestLockId = entry.Key;
                    latestLoadingState = entry.Value;
                }
            }

            if (latestLoadingState != null)
            {
                blockerImage_.ShowLoading(
                    latestLoadingState.LoadingMessage,
                    latestLoadingState.ShowLoadingWithTime,
                    latestLoadingState.StartedAtRealtime);
            }
            else
            {
                blockerImage_.HideLoading();
            }
        }

        private sealed class Scope : IDisposable
        {
            private ScreenBlocker owner_;
            private readonly int id_;

            public Scope(ScreenBlocker owner, int id)
            {
                owner_ = owner;
                id_ = id;
            }

            public void Dispose()
            {
                if (owner_ == null)
                {
                    return;
                }

                owner_.Unblock(id_);
                owner_ = null;
            }
        }
    }
}
