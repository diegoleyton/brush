using Flowbit.Utilities.Navigation;
using Game.Unity.Definitions;
using Game.Unity.Definitions.Events;
using Flowbit.Utilities.Coroutines;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.UI;
using UnityEngine.SceneManagement;
using System;


namespace Game.Unity.Scenes
{
    /// <summary>
    /// Scene service for this game using transition
    /// </summary>
    public class GameNavigationService : IGameNavigationService
    {
        private readonly INavigationService navigationService_;
        private readonly SceneSettings sceneSettings_;

        private readonly ICoroutineService coroutineService_;

        private readonly EventDispatcher eventDispatcher_;

        private readonly ScreenBlocker screenBlocker_;

        private IDisposable navigationBlock_;

        public GameNavigationService(
            INavigationService navigationService,
            SceneSettings sceneSettings,
            ICoroutineService coroutineService,
            EventDispatcher eventDispatcher,
            ScreenBlocker screenBlocker)
        {
            navigationService_ = navigationService;
            sceneSettings_ = sceneSettings;
            coroutineService_ = coroutineService;
            coroutineService_ = coroutineService;
            eventDispatcher_ = eventDispatcher;
            screenBlocker_ = screenBlocker;


            string sceneName = SceneManager.GetActiveScene().name;
            navigationService_.StartWith(sceneSettings_.GetTarget(sceneName));
            eventDispatcher_.Subscribe<NavigationTransitionStartedEvent>(OnNavigationTransitionStarted);
            eventDispatcher_.Subscribe<NavigationTransitionFinishedEvent>(OnNavigationTransitionFinished);
        }

        /// <summary>
        /// Navigates to the given type
        /// </summary>
        public void Navigate(
            SceneType sceneType,
            NavigationParams navigationParams = null)
        {
            var target = sceneSettings_.GetTarget(sceneType);
            coroutineService_.StartCoroutine(
                navigationService_.Navigate(
                    target,
                    navigationParams));

            if (target.TargetType == NavigationTargetType.Scene)
            {
                eventDispatcher_?.Send(new OnNextScene(sceneType));
            }
            else
            {
                eventDispatcher_?.Send(new OnPopupOpen());
            }
        }

        /// <summary>
        /// Navigates to the previous node
        /// </summary>
        public void Back()
        {
            coroutineService_.StartCoroutine(navigationService_.Back());
            eventDispatcher_?.Send(new OnPreviousScene());
        }

        /// <summary>
        /// Closes the opened prefab with the given id.
        /// </summary>
        public void Close(SceneType sceneType)
        {
            string id = sceneSettings_.GetTarget(sceneType).Id;
            coroutineService_.StartCoroutine(navigationService_.Close(id));
            eventDispatcher_?.Send(new OnPopupClose());
        }

        /// <summary>
        /// Navigates to the previous node
        /// </summary>
        public bool CanGoBack => navigationService_.CanGoBack;

        private void OnNavigationTransitionStarted(NavigationTransitionStartedEvent e)
        {
            navigationBlock_ = screenBlocker_.BlockScope("Navigation");
        }

        private void OnNavigationTransitionFinished(NavigationTransitionFinishedEvent e)
        {
            navigationBlock_?.Dispose();
            navigationBlock_ = null;
        }
    }
}
