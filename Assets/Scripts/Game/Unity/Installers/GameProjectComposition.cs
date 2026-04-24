using System;

using Flowbit.Utilities.Audio;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.Coroutines;
using Flowbit.Utilities.Navigation;
using Flowbit.Utilities.Storage;
using Flowbit.Utilities.Unity.UI;

using Game.Core.DataController;
using Game.Core.Services;
using Game.Unity.Audio;
using Game.Unity.Scenes;
using Game.Unity.Scenes.Transitions;
using Game.Core.Data;

using UnityEngine;

namespace Game.Unity.Installers
{
    /// <summary>
    /// DI-agnostic composition helper for the game's Unity services.
    /// </summary>
    public sealed class GameProjectComposition
    {
        private readonly SceneSettings sceneSettings_;
        private readonly GameSoundLibrary gameSoundLibrary_;

        /// <summary>
        /// Creates a new composition helper for the given game resources.
        /// </summary>
        public GameProjectComposition(SceneSettings sceneSettings, GameSoundLibrary gameSoundLibrary)
        {
            sceneSettings_ = sceneSettings ?? throw new ArgumentNullException(nameof(sceneSettings));
            gameSoundLibrary_ = gameSoundLibrary ?? throw new ArgumentNullException(nameof(gameSoundLibrary));
        }

        /// <summary>
        /// Applies device-level bootstrap configuration.
        /// </summary>
        public void PreventScreenSleep()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        /// <summary>
        /// Creates the default local data storage implementation.
        /// </summary>
        public IDataStorage CreateDataStorage()
        {
            return new PlayerPrefsDataStorage();
        }

        /// <summary>
        /// Creates the shared event dispatcher.
        /// </summary>
        public EventDispatcher CreateEventDispatcher() => new EventDispatcher();

        /// <summary>
        /// Creates the root runtime game data container.
        /// </summary>
        public Data CreateGameData(IDataStorage dataStorage)
        {
            if (dataStorage == null)
            {
                return new Data();
            }

            DataLoadResult<Data> loadResult =
                dataStorage.LoadAsync<Data>(PersistentGameDataService.GameDataStorageKey).GetAwaiter().GetResult();

            return loadResult.Data ?? new Data();
        }

        /// <summary>
        /// Creates the runtime data repository.
        /// </summary>
        public DataRepository CreateDataRepository(Data data, EventDispatcher dispatcher, IGameLogger logger)
        {
            return new DataRepository(data, dispatcher, logger);
        }

        /// <summary>
        /// Creates the runtime data controller.
        /// </summary>
        public DataController CreateDataController(DataRepository repository, EventDispatcher dispatcher)
        {
            return new DataController(repository, dispatcher);
        }

        /// <summary>
        /// Creates the persistent coroutine service.
        /// </summary>
        public ICoroutineService CreateCoroutineService()
        {
            GameObject go = new GameObject("[CoroutineService]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go.AddComponent<CoroutineService>();
        }

        /// <summary>
        /// Creates the persistent screen blocker service.
        /// </summary>
        public ScreenBlocker CreateScreenBlocker()
        {
            ScreenBlockerImage blockerImage = CreatePersistentPrefab(sceneSettings_.ScreenBlockerImage);
            return new ScreenBlocker(blockerImage.Image);
        }

        /// <summary>
        /// Creates the base navigation service.
        /// </summary>
        public INavigationService CreateNavigationService(EventDispatcher dispatcher)
        {
            GameSceneTransitionBase sceneTransitionNext =
                CreatePersistentPrefab(sceneSettings_.TransitionNext);
            GameSceneTransitionBase sceneTransitionPrev =
                CreatePersistentPrefab(sceneSettings_.TransitionPrev);
            GameSceneTransitionBase popupTransitionOpen =
                CreatePersistentPrefab(sceneSettings_.PopupTransitionOpen);
            GameSceneTransitionBase popupTransitionClose =
                CreatePersistentPrefab(sceneSettings_.PopupTransitionClose);

            GameObject navigationPopupContainer = new GameObject("[NavigationPopups]");
            UnityEngine.Object.DontDestroyOnLoad(navigationPopupContainer);

            return new NavigationService(
                sceneSettings_.GetPrefabs(),
                navigationPopupContainer.transform,
                new SceneNavigationNodeResolver(),
                sceneTransitionNext,
                sceneTransitionPrev,
                popupTransitionOpen,
                popupTransitionClose,
                dispatcher);
        }

        /// <summary>
        /// Creates the game-specific navigation facade.
        /// </summary>
        public IGameNavigationService CreateGameNavigationService(
            INavigationService navigationService,
            ICoroutineService coroutineService,
            EventDispatcher dispatcher,
            ScreenBlocker screenBlocker)
        {
            return new GameNavigationService(
                navigationService,
                sceneSettings_,
                coroutineService,
                dispatcher,
                screenBlocker);
        }

        /// <summary>
        /// Creates the audio player used by the game.
        /// </summary>
        public AudioPlayer<SoundId> CreateAudioPlayer(ICoroutineService coroutineService)
        {
            GameObject audioPool = UnityEngine.Object.Instantiate(gameSoundLibrary_.AudioPool);
            UnityEngine.Object.DontDestroyOnLoad(audioPool);

            return new AudioPlayer<SoundId>(
                gameSoundLibrary_,
                audioPool.GetComponent<IAudioSourcePool>(),
                audioPool.GetComponent<ILoopingAudioSourcePool>(),
                coroutineService);
        }

        /// <summary>
        /// Creates the audio event bridge.
        /// </summary>
        public AudioReactor CreateAudioReactor(EventDispatcher dispatcher, AudioPlayer<SoundId> audioPlayer)
        {
            return new AudioReactor(dispatcher, audioPlayer);
        }

        private static T CreatePersistentPrefab<T>(T prefab) where T : Component
        {
            if (prefab == null)
            {
                //throw new InvalidOperationException($"Cannot instantiate missing prefab of type {typeof(T).Name}.");
                return null;
            }

            T instance = UnityEngine.Object.Instantiate(prefab);
            UnityEngine.Object.DontDestroyOnLoad(instance.gameObject);
            return instance;
        }
    }
}
