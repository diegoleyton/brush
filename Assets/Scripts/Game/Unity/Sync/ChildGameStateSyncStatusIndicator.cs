using Flowbit.Utilities.Core.Events;
using Game.Core.Events;
using Game.Core.Services;
using UnityEngine;
using Zenject;

namespace Game.Unity.Sync
{
    /// <summary>
    /// Serialized UI component for showing child game state sync status in any scene.
    /// </summary>
    public sealed class ChildGameStateSyncStatusIndicator : MonoBehaviour
    {
        [SerializeField]
        private GameObject offlineIconRoot_;

        [SerializeField]
        private GameObject pendingSyncIconRoot_;

        private EventDispatcher dispatcher_;
        private IChildGameStateSyncService syncService_;
        private bool subscribed_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IChildGameStateSyncService syncService)
        {
            dispatcher_ = dispatcher;
            syncService_ = syncService;
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<ChildGameStateSyncStatusChangedEvent>(OnSyncStatusChanged);
            subscribed_ = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<ChildGameStateSyncStatusChangedEvent>(OnSyncStatusChanged);
            subscribed_ = false;
        }

        private void OnSyncStatusChanged(ChildGameStateSyncStatusChangedEvent _)
        {
            Refresh();
        }

        private void Refresh()
        {
            bool isOffline = syncService_ != null && syncService_.IsOffline;
            bool hasPendingSync = syncService_ != null && syncService_.HasPendingSync;

            if (offlineIconRoot_ != null)
            {
                offlineIconRoot_.SetActive(isOffline);
            }

            if (pendingSyncIconRoot_ != null)
            {
                pendingSyncIconRoot_.SetActive(!isOffline && hasPendingSync);
            }
        }
    }
}
