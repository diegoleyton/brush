using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.Instantiator;

namespace Flowbit.Utilities.Navigation
{
    /// <summary>
    /// Navigates between scene and prefab nodes.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        private readonly IReadOnlyDictionary<string, GameObject> prefabMap_;
        private readonly Transform prefabParent_;
        private readonly INavigationNodeResolver sceneNodeResolver_;
        private readonly INavigationTransitionStrategy navigateTransitionStrategy_;
        private readonly INavigationTransitionStrategy backTransitionStrategy_;
        private readonly INavigationTransitionStrategy popupOpenTransitionStrategy_;
        private readonly INavigationTransitionStrategy popupCloseTransitionStrategy_;
        private readonly EventDispatcher eventDispatcher_;
        private readonly IObjectInstantiator prefabInstantiator_;
        private readonly Stack<NavigationHistoryEntry> sceneHistory_;
        private readonly Dictionary<string, ResolvedNavigationNode> openedPrefabs_;
        private readonly Dictionary<string, GameObject> openedPrefabInstances_;

        private ResolvedNavigationNode currentSceneNode_;
        private int activeTransitionCount_;

        /// <summary>
        /// Creates a new navigation service.
        /// </summary>
        public NavigationService(
            IReadOnlyDictionary<string, GameObject> prefabMap,
            Transform prefabParent,
            INavigationNodeResolver sceneNodeResolver,
            INavigationTransitionStrategy navigateTransitionStrategy,
            INavigationTransitionStrategy backTransitionStrategy,
            INavigationTransitionStrategy popupOpenTransitionStrategy,
            INavigationTransitionStrategy popupCloseTransitionStrategy,
            EventDispatcher eventDispatcher,
            IObjectInstantiator prefabInstantiator = null)
        {
            prefabMap_ = prefabMap ?? throw new ArgumentNullException(nameof(prefabMap));
            prefabParent_ = prefabParent;
            sceneNodeResolver_ = sceneNodeResolver ?? throw new ArgumentNullException(nameof(sceneNodeResolver));
            navigateTransitionStrategy_ = navigateTransitionStrategy;
            backTransitionStrategy_ = backTransitionStrategy;
            popupOpenTransitionStrategy_ = popupOpenTransitionStrategy;
            popupCloseTransitionStrategy_ = popupCloseTransitionStrategy;
            eventDispatcher_ = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            prefabInstantiator_ = prefabInstantiator;

            sceneHistory_ = new Stack<NavigationHistoryEntry>();
            openedPrefabs_ = new Dictionary<string, ResolvedNavigationNode>();
            openedPrefabInstances_ = new Dictionary<string, GameObject>();
        }

        /// <summary>
        /// Gets whether the navigator can go back to a previous scene.
        /// </summary>
        public bool CanGoBack => sceneHistory_.Count > 0;

        /// <summary>
        /// Sets the initial current node without performing a navigation transition.
        /// </summary>
        public void StartWith(NavigationTarget target, NavigationParams navigationParams = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (currentSceneNode_ != null)
            {
                throw new InvalidOperationException("The navigation service has already been started.");
            }

            if (sceneHistory_.Count > 0)
            {
                throw new InvalidOperationException("Cannot start navigation with a non-empty scene history.");
            }

            if (target.TargetType != NavigationTargetType.Scene)
            {
                throw new InvalidOperationException("StartWith only supports scene targets.");
            }

            StartWithSceneTarget(target, navigationParams);
        }

        /// <summary>
        /// Navigates to the given target.
        /// </summary>
        public IEnumerator Navigate(NavigationTarget target, NavigationParams navigationParams = null)
        {
            return NavigateInternal(target, navigationParams, addCurrentSceneToHistory: true);
        }

        /// <summary>
        /// Navigates to the given target without adding the current scene to navigation history.
        /// </summary>
        public IEnumerator NavigateWithoutHistory(NavigationTarget target, NavigationParams navigationParams = null)
        {
            return NavigateInternal(target, navigationParams, addCurrentSceneToHistory: false);
        }

        /// <summary>
        /// Navigates to the given scene target and clears the back stack so the destination becomes the new root.
        /// </summary>
        public IEnumerator NavigateAsRoot(NavigationTarget target, NavigationParams navigationParams = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.TargetType != NavigationTargetType.Scene)
            {
                throw new InvalidOperationException("NavigateAsRoot only supports scene targets.");
            }

            sceneHistory_.Clear();
            return NavigateInternal(target, navigationParams, addCurrentSceneToHistory: false);
        }

        private IEnumerator NavigateInternal(
            NavigationTarget target,
            NavigationParams navigationParams,
            bool addCurrentSceneToHistory)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            using (BeginTransitionScope())
            {
                switch (target.TargetType)
                {
                    case NavigationTargetType.Scene:
                        yield return NavigateToScene(target, navigationParams, addCurrentSceneToHistory);
                        yield break;

                    case NavigationTargetType.Prefab:
                        yield return NavigateToPrefab(target, navigationParams);
                        yield break;

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported navigation target type '{target.TargetType}'.");
                }
            }
        }

        /// <summary>
        /// Navigates back to the previous scene in the history.
        /// </summary>
        public IEnumerator Back()
        {
            if (!CanGoBack)
            {
                yield break;
            }

            using (BeginTransitionScope())
            {
                NavigationHistoryEntry previousEntry = sceneHistory_.Pop();

                NavigationTransitionContext prepareContext = new NavigationTransitionContext(
                    currentSceneNode_?.Target,
                    currentSceneNode_?.Node,
                    previousEntry.NavigationParams);

                if (backTransitionStrategy_ != null)
                {
                    yield return backTransitionStrategy_.PrepareTransition(prepareContext);
                }

                CloseAllOpenedPrefabsImmediately();

                ResolvedNavigationNode previousSceneNode = null;

                yield return ResolveSceneTarget(
                    previousEntry.Target,
                    previousEntry.NavigationParams,
                    shouldInitialize: true,
                    onResolved: resolvedNode => previousSceneNode = resolvedNode);

                currentSceneNode_ = previousSceneNode;

                NavigationTransitionContext finishContext = new NavigationTransitionContext(
                    previousSceneNode.Target,
                    previousSceneNode.Node,
                    previousEntry.NavigationParams);

                if (backTransitionStrategy_ != null)
                {
                    yield return backTransitionStrategy_.FinishTransition(finishContext);
                }
            }
        }

        /// <summary>
        /// Closes the opened prefab with the given id.
        /// </summary>
        public IEnumerator Close(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Prefab id cannot be null or empty.", nameof(id));
            }

            if (!openedPrefabs_.TryGetValue(id, out ResolvedNavigationNode prefabNode))
            {
                yield break;
            }

            using (BeginTransitionScope())
            {
                NavigationTransitionContext prepareContext =
                    new NavigationTransitionContext(
                        prefabNode.Target,
                        prefabNode.Node,
                        prefabNode.NavigationParams);

                if (popupCloseTransitionStrategy_ != null)
                {
                    yield return popupCloseTransitionStrategy_.PrepareTransition(prepareContext);
                }

                DisposePrefab(id);

                NavigationTransitionContext finishContext =
                    new NavigationTransitionContext(
                        currentSceneNode_?.Target,
                        currentSceneNode_?.Node,
                        prefabNode.NavigationParams);

                if (popupCloseTransitionStrategy_ != null)
                {
                    yield return popupCloseTransitionStrategy_.FinishTransition(finishContext);
                }
            }
        }

        private IDisposable BeginTransitionScope()
        {
            activeTransitionCount_++;
            if (activeTransitionCount_ == 1)
            {
                eventDispatcher_.Send(this, new NavigationTransitionStartedEvent());
            }

            return new TransitionScope(this);
        }

        private void EndTransitionScope()
        {
            if (activeTransitionCount_ <= 0)
            {
                throw new InvalidOperationException("Transition scope count cannot go below zero.");
            }

            activeTransitionCount_--;
            if (activeTransitionCount_ == 0)
            {
                eventDispatcher_.Send(this, new NavigationTransitionFinishedEvent());
            }
        }

        private sealed class TransitionScope : IDisposable
        {
            private NavigationService owner_;

            public TransitionScope(NavigationService owner)
            {
                owner_ = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void Dispose()
            {
                if (owner_ == null)
                {
                    return;
                }

                owner_.EndTransitionScope();
                owner_ = null;
            }
        }

        private void StartWithSceneTarget(NavigationTarget target, NavigationParams navigationParams)
        {
            INavigationNode node = sceneNodeResolver_.Resolve(target);
            if (node == null)
            {
                throw new InvalidOperationException(
                    $"No INavigationNode was resolved for scene '{target.Id}'.");
            }

            InitializeNodeIfNeeded(node, navigationParams, forceInitialize: false);
            currentSceneNode_ = new ResolvedNavigationNode(target, node, navigationParams);
        }

        private IEnumerator NavigateToScene(
            NavigationTarget target,
            NavigationParams navigationParams,
            bool addCurrentSceneToHistory)
        {
            NavigationTransitionContext prepareContext =
                new NavigationTransitionContext(currentSceneNode_?.Target, currentSceneNode_?.Node, navigationParams);

            if (navigateTransitionStrategy_ != null)
            {
                yield return navigateTransitionStrategy_.PrepareTransition(prepareContext);
            }

            if (addCurrentSceneToHistory && currentSceneNode_ != null)
            {
                sceneHistory_.Push(
                    new NavigationHistoryEntry(currentSceneNode_.Target, currentSceneNode_.NavigationParams));
            }

            CloseAllOpenedPrefabsImmediately();

            ResolvedNavigationNode nextSceneNode = null;
            yield return ResolveSceneTarget(
                target,
                navigationParams,
                shouldInitialize: true,
                onResolved: resolvedNode => nextSceneNode = resolvedNode);

            currentSceneNode_ = nextSceneNode;

            NavigationTransitionContext finishContext =
                new NavigationTransitionContext(nextSceneNode.Target, nextSceneNode.Node, navigationParams);

            if (navigateTransitionStrategy_ != null)
            {
                yield return navigateTransitionStrategy_.FinishTransition(finishContext);
            }
        }

        private IEnumerator NavigateToPrefab(NavigationTarget target, NavigationParams navigationParams)
        {
            if (openedPrefabs_.ContainsKey(target.Id))
            {
                yield break;
            }

            NavigationTransitionContext prepareContext =
                new NavigationTransitionContext(currentSceneNode_?.Target, currentSceneNode_?.Node, navigationParams);

            if (popupOpenTransitionStrategy_ != null)
            {
                yield return popupOpenTransitionStrategy_.PrepareTransition(prepareContext);
            }

            ResolvedNavigationNode prefabNode = null;
            GameObject prefabInstance = null;

            ResolvePrefabTarget(
                target,
                navigationParams,
                shouldInitialize: true,
                onResolved: resolvedNode => prefabNode = resolvedNode,
                onPrefabInstanceCreated: instance => prefabInstance = instance);

            openedPrefabs_[target.Id] = prefabNode;
            openedPrefabInstances_[target.Id] = prefabInstance;

            NavigationTransitionContext finishContext =
                new NavigationTransitionContext(prefabNode.Target, prefabNode.Node, navigationParams);

            if (popupOpenTransitionStrategy_ != null)
            {
                yield return popupOpenTransitionStrategy_.FinishTransition(finishContext);
            }
        }

        private IEnumerator ResolveSceneTarget(
            NavigationTarget target,
            NavigationParams navigationParams,
            bool shouldInitialize,
            Action<ResolvedNavigationNode> onResolved)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(target.Id, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                throw new InvalidOperationException($"Could not load scene '{target.Id}'.");
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            INavigationNode node = sceneNodeResolver_.Resolve(target);
            if (node == null)
            {
                throw new InvalidOperationException(
                    $"No INavigationNode was resolved for scene '{target.Id}'.");
            }

            InitializeNodeIfNeeded(node, navigationParams, shouldInitialize);
            onResolved(new ResolvedNavigationNode(target, node, navigationParams));
        }

        private void ResolvePrefabTarget(
            NavigationTarget target,
            NavigationParams navigationParams,
            bool shouldInitialize,
            Action<ResolvedNavigationNode> onResolved,
            Action<GameObject> onPrefabInstanceCreated)
        {
            if (!prefabMap_.TryGetValue(target.Id, out GameObject prefab) || prefab == null)
            {
                throw new InvalidOperationException(
                    $"No prefab was registered for navigation target '{target.Id}'.");
            }

            GameObject instance = prefabInstantiator_ != null
                ? prefabInstantiator_.InstantiatePrefab(prefab, prefabParent_)
                : UnityEngine.Object.Instantiate(prefab, prefabParent_);

            if (!instance.TryGetComponent(out INavigationNode node))
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"Prefab navigation target '{target.Id}' does not implement INavigationNode.");
            }

            InitializeNodeIfNeeded(node, navigationParams, shouldInitialize);

            onPrefabInstanceCreated(instance);
            onResolved(new ResolvedNavigationNode(target, node, navigationParams));
        }

        private static void InitializeNodeIfNeeded(
            INavigationNode node,
            NavigationParams navigationParams,
            bool forceInitialize)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (forceInitialize || navigationParams != null)
            {
                node.Initialize(navigationParams);
            }
        }

        private void CloseAllOpenedPrefabsImmediately()
        {
            DisposeAllOpenedPrefabs();
        }

        private void DisposePrefab(string id)
        {
            if (openedPrefabInstances_.TryGetValue(id, out GameObject instance))
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }

                openedPrefabInstances_.Remove(id);
            }

            openedPrefabs_.Remove(id);
        }

        private void DisposeAllOpenedPrefabs()
        {
            foreach (KeyValuePair<string, GameObject> pair in openedPrefabInstances_)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value);
                }
            }

            openedPrefabInstances_.Clear();
            openedPrefabs_.Clear();
        }
    }
}
