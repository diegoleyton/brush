using Flowbit.Utilities.Core.Events;
using Game.Core.Events;
using Game.Unity.Definitions;
using Game.Unity.Scenes;
using Zenject;

namespace Game.Unity.Sync
{
    /// <summary>
    /// Presents non-blocking child game state sync failures through a global error popup.
    /// </summary>
    public sealed class ChildGameStateSyncFailurePresenter : IInitializable, System.IDisposable
    {
        private readonly EventDispatcher dispatcher_;
        private readonly IGameNavigationService navigationService_;

        public ChildGameStateSyncFailurePresenter(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService)
        {
            dispatcher_ = dispatcher;
            navigationService_ = navigationService;
        }

        public void Initialize()
        {
            dispatcher_?.Subscribe<ChildGameStateSyncFailureEvent>(OnSyncFailure);
        }

        public void Dispose()
        {
            dispatcher_?.Unsubscribe<ChildGameStateSyncFailureEvent>(OnSyncFailure);
        }

        private void OnSyncFailure(ChildGameStateSyncFailureEvent eventData)
        {
            if (navigationService_ == null || eventData == null)
            {
                return;
            }

            navigationService_.Navigate(
                SceneType.ErrorPopup,
                new ErrorPopupParams(eventData.Message, title: "Could not save changes"));
        }
    }
}
