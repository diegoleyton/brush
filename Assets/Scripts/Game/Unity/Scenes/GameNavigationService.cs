using Flowbit.Utilities.Navigation;
using Game.Unity.Definitions;
using Game.Unity.Definitions.Events;
using Flowbit.Utilities.Coroutines;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Collections;


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
        private readonly Queue<PendingNavigationRequest> pendingRequests_ = new();
        private bool sceneTransitionInProgress_;

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
            EnqueueOrExecute(
                new PendingNavigationRequest(
                    PendingNavigationRequestType.Navigate,
                    sceneType,
                    navigationParams,
                    addCurrentSceneToHistory: true));
        }

        /// <summary>
        /// Navigates to the given type without adding the current scene to navigation history.
        /// </summary>
        public void NavigateWithoutHistory(
            SceneType sceneType,
            NavigationParams navigationParams = null)
        {
            EnqueueOrExecute(
                new PendingNavigationRequest(
                    PendingNavigationRequestType.Navigate,
                    sceneType,
                    navigationParams,
                    addCurrentSceneToHistory: false));
        }

        public void NavigateAsRoot(
            SceneType sceneType,
            NavigationParams navigationParams = null)
        {
            EnqueueOrExecute(
                new PendingNavigationRequest(
                    PendingNavigationRequestType.NavigateAsRoot,
                    sceneType,
                    navigationParams,
                    addCurrentSceneToHistory: false));
        }

        /// <summary>
        /// Navigates to the previous node
        /// </summary>
        public void Back()
        {
            EnqueueOrExecute(new PendingNavigationRequest(PendingNavigationRequestType.Back));
        }

        /// <summary>
        /// Closes the opened prefab with the given id.
        /// </summary>
        public void Close(SceneType sceneType)
        {
            EnqueueOrExecute(new PendingNavigationRequest(PendingNavigationRequestType.Close, sceneType));
        }

        /// <summary>
        /// Navigates to the previous node
        /// </summary>
        public bool CanGoBack => navigationService_.CanGoBack;

        private void EnqueueOrExecute(PendingNavigationRequest request)
        {
            if (sceneTransitionInProgress_)
            {
                pendingRequests_.Enqueue(request);
                return;
            }

            ExecuteRequest(request);
        }

        private void ExecuteRequest(PendingNavigationRequest request)
        {
            switch (request.RequestType)
            {
                case PendingNavigationRequestType.Navigate:
                    NavigateInternal(
                        request.SceneType,
                        request.NavigationParams,
                        request.AddCurrentSceneToHistory);
                    break;

                case PendingNavigationRequestType.NavigateAsRoot:
                    NavigateAsRootInternal(request.SceneType, request.NavigationParams);
                    break;

                case PendingNavigationRequestType.Back:
                    StartSceneTransition();
                    coroutineService_.StartCoroutine(RunSceneNavigation(navigationService_.Back()));
                    eventDispatcher_?.Send(new OnPreviousScene());
                    break;

                case PendingNavigationRequestType.Close:
                    CloseInternal(request.SceneType);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported navigation request type '{request.RequestType}'.");
            }
        }

        private void NavigateInternal(
            SceneType sceneType,
            NavigationParams navigationParams,
            bool addCurrentSceneToHistory)
        {
            NavigationTarget target = sceneSettings_.GetTarget(sceneType);

            if (target.TargetType == NavigationTargetType.Scene)
            {
                StartSceneTransition();
            }

            var navigationRoutine =
                addCurrentSceneToHistory
                    ? navigationService_.Navigate(target, navigationParams)
                    : navigationService_.NavigateWithoutHistory(target, navigationParams);

            coroutineService_.StartCoroutine(
                target.TargetType == NavigationTargetType.Scene
                    ? RunSceneNavigation(navigationRoutine)
                    : navigationRoutine);

            if (target.TargetType == NavigationTargetType.Scene)
            {
                eventDispatcher_?.Send(new OnNextScene(sceneType));
            }
            else
            {
                eventDispatcher_?.Send(new OnPopupOpen());
            }
        }

        private void NavigateAsRootInternal(SceneType sceneType, NavigationParams navigationParams)
        {
            NavigationTarget target = sceneSettings_.GetTarget(sceneType);

            if (target.TargetType == NavigationTargetType.Scene)
            {
                StartSceneTransition();
            }

            coroutineService_.StartCoroutine(
                target.TargetType == NavigationTargetType.Scene
                    ? RunSceneNavigation(navigationService_.NavigateAsRoot(target, navigationParams))
                    : navigationService_.NavigateAsRoot(target, navigationParams));

            if (target.TargetType == NavigationTargetType.Scene)
            {
                eventDispatcher_?.Send(new OnNextScene(sceneType));
            }
            else
            {
                eventDispatcher_?.Send(new OnPopupOpen());
            }
        }

        private void CloseInternal(SceneType sceneType)
        {
            string id = sceneSettings_.GetTarget(sceneType).Id;
            coroutineService_.StartCoroutine(navigationService_.Close(id));
            eventDispatcher_?.Send(new OnPopupClose());
        }

        private void StartSceneTransition()
        {
            sceneTransitionInProgress_ = true;
        }

        private IEnumerator RunSceneNavigation(IEnumerator routine)
        {
            try
            {
                yield return routine;
            }
            finally
            {
                sceneTransitionInProgress_ = false;
                ProcessNextQueuedRequestIfNeeded();
            }
        }

        private void OnNavigationTransitionStarted(NavigationTransitionStartedEvent e)
        {
            navigationBlock_ = screenBlocker_.BlockScope("Navigation");
        }

        private void OnNavigationTransitionFinished(NavigationTransitionFinishedEvent e)
        {
            navigationBlock_?.Dispose();
            navigationBlock_ = null;
        }

        private void ProcessNextQueuedRequestIfNeeded()
        {
            if (sceneTransitionInProgress_ || pendingRequests_.Count == 0)
            {
                return;
            }

            PendingNavigationRequest nextRequest = pendingRequests_.Dequeue();
            try
            {
                ExecuteRequest(nextRequest);
            }
            finally
            {
            }
        }

        private enum PendingNavigationRequestType
        {
            Navigate,
            NavigateAsRoot,
            Back,
            Close
        }

        private readonly struct PendingNavigationRequest
        {
            public PendingNavigationRequestType RequestType { get; }
            public SceneType SceneType { get; }
            public NavigationParams NavigationParams { get; }
            public bool AddCurrentSceneToHistory { get; }

            public PendingNavigationRequest(PendingNavigationRequestType requestType)
            {
                RequestType = requestType;
                SceneType = default;
                NavigationParams = null;
                AddCurrentSceneToHistory = true;
            }

            public PendingNavigationRequest(PendingNavigationRequestType requestType, SceneType sceneType)
            {
                RequestType = requestType;
                SceneType = sceneType;
                NavigationParams = null;
                AddCurrentSceneToHistory = true;
            }

            public PendingNavigationRequest(
                PendingNavigationRequestType requestType,
                SceneType sceneType,
                NavigationParams navigationParams,
                bool addCurrentSceneToHistory)
            {
                RequestType = requestType;
                SceneType = sceneType;
                NavigationParams = navigationParams;
                AddCurrentSceneToHistory = addCurrentSceneToHistory;
            }
        }
    }
}
