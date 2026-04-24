using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Flowbit.Utilities.Unity.UI
{
    /// <summary>
    /// Blocks user input using a full-screen image, supporting multiple concurrent locks.
    /// </summary>
    public class ScreenBlocker
    {
        private readonly Image blockerImage_;
        private readonly Dictionary<int, string> activeLocks_ = new();
        private int idCounter_;

        public bool IsBlocked => activeLocks_.Count > 0;

        public ScreenBlocker(Image blockerImage)
        {
            blockerImage_ = blockerImage;

            if (blockerImage_ != null)
            {
                blockerImage_.raycastTarget = true; // importante
                blockerImage_.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Blocks the screen and returns a disposable scope that will unblock automatically.
        /// </summary>
        public IDisposable BlockScope(string reason = "")
        {
            int id = Block(reason);
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

        private int Block(string reason = "")
        {
            int id = ++idCounter_;
            activeLocks_[id] = reason;

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
            if (blockerImage_ != null)
            {
                blockerImage_.gameObject.SetActive(activeLocks_.Count > 0);
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