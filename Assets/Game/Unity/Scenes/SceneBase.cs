using UnityEngine;
using System;
using System.Collections;
using Game.Unity.Definitions;
using Game.Unity.Definitions.Events;
using Flowbit.Utilities.Navigation;
using Flowbit.Utilities.Core.Events;
using Zenject;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Base MonoBehaviour implementation for screens.
    /// </summary>
    public abstract class SceneBase : MonoBehaviour, INavigationNode
    {
        private EventDispatcher dispatcher_;
        private IGameNavigationService navigationService_;

        [SerializeField]
        protected SceneType sceneType_;

        protected bool initialized_ = false;

        public string Id => sceneType_.GeTInstruction();

        private NavigationParams sceneParameters_;

        private static bool firstScene_ = true;

        protected virtual void Start()
        {
            EnsureDependencies();

            if (!firstScene_)
            {
                return;
            }

            firstScene_ = false;
            dispatcher_.Send(new OnFirstScene());
            StartCoroutine(InitializeByDefault());
        }

        /// <summary>
        /// Sets the dependencies required by the scene.
        /// </summary>
        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService)
        {
            dispatcher_ = dispatcher;
            navigationService_ = navigationService;
        }

        protected T GetSceneParameters<T>() where T : NavigationParams
        {
            if (sceneParameters_ is not T parameters)
            {
                throw new ArgumentException(
                    $"Expected {nameof(T)} but got {sceneParameters_?.GetType().Name ?? "null"}.");
            }

            return parameters;
        }

        /// <summary>
        /// Initializes the screen with navigation parameters.
        /// </summary>
        public void Initialize(NavigationParams navigationParams)
        {
            sceneParameters_ = navigationParams;
            Initialize();
            initialized_ = true;
        }

        protected virtual void Initialize() { }

        protected IGameNavigationService NavigationService => navigationService_;

        private IEnumerator InitializeByDefault()
        {
            yield return null;
            yield return null;
            if (!initialized_)
            {
                Initialize(null);
            }
        }

        private void EnsureDependencies()
        {
            if (dispatcher_ == null || navigationService_ == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} dependencies have not been configured. Call {nameof(Construct)} before using the scene.");
            }
        }
    }
}
