using System;

using Flowbit.Utilities.Audio;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.Coroutines;
using Flowbit.Utilities.Navigation;
using Flowbit.Utilities.Storage;
using Flowbit.Utilities.Unity.AssetLoader;
using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.Logger;
using Flowbit.Utilities.Unity.UI;

using Game.Core.DataController;
using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Audio;
using Game.Unity.Development;
using Game.Unity.Definitions;
using Game.Unity.Instantiator;
using Game.Unity.RoomScene;
using Game.Unity.Scenes;
using Game.Unity.Settings;

using Zenject;

namespace Game.Unity.Installers
{
    /// <summary>
    /// Registers the persistent game-wide Unity services in Zenject.
    /// </summary>
    public sealed class GameProjectInstaller : MonoInstaller
    {
        private const string SceneSettingsResourcePath = "SceneSettings";
        private const string GameSoundLibraryResourcePath = "GameSoundLibrary";
        private const string DevelopmentProfileSettingsResourcePath = "DevelopmentProfileSettings";
        private const string RoomSettingsResourcePath = "RoomSettings";

        public override void InstallBindings()
        {
            SceneSettings sceneSettings = LoadSceneSettings();
            GameSoundLibrary gameSoundLibrary = LoadGameSoundLibrary();
            DevelopmentProfileSettings developmentProfileSettings = LoadDevelopmentProfileSettings();
            RoomSettings roomSettings = LoadRoomSettings();
            GameProjectComposition composition = new GameProjectComposition(sceneSettings, gameSoundLibrary);

            composition.PreventScreenSleep();

            Container.BindInstance(composition).AsSingle();
            Container.BindInstance(sceneSettings).AsSingle();
            Container.BindInstance(gameSoundLibrary).AsSingle();
            Container.BindInstance(developmentProfileSettings).AsSingle();
            Container.BindInstance(roomSettings).AsSingle();

            BindCoreServices(composition);
            BindGameData(composition);
            BindNavigation(composition);
            BindAudio(composition);
            BindDevelopmentServices();
        }

        private void BindCoreServices(GameProjectComposition composition)
        {
            Container.Bind<IObjectInstantiator>()
                .WithId(InstantiatorIds.Unity)
                .To<UnityInstantiator>()
                .AsSingle();

            Container.Bind<IObjectInstantiator>()
                .WithId(InstantiatorIds.Dependency)
                .To<DependencyObjectInstantiator>()
                .AsSingle();

            Container.Bind<IDataStorage>()
                .FromMethod(_ => composition.CreateDataStorage())
                .AsSingle();

            Container.Bind<EventDispatcher>()
                .FromMethod(_ => composition.CreateEventDispatcher())
                .AsSingle();

            Container.Bind<IGameLogger>()
                .To<UnityGameLogger>()
                .AsSingle();

            Container.Bind<IAssetLoader>()
                .To<AddressablesAssetLoader>()
                .AsSingle();

            Container.BindFactory<System.Collections.Generic.IReadOnlyList<PetEyesSurfaceView>, PetEyesController, PetEyesController.Factory>();
            Container.BindFactory<System.Collections.Generic.IReadOnlyList<PetHatSurfaceView>, PetHatController, PetHatController.Factory>();
            Container.BindFactory<System.Collections.Generic.IReadOnlyList<PetSkinSurfaceView>, PetSkinController, PetSkinController.Factory>();
            Container.BindFactory<System.Collections.Generic.IReadOnlyList<PetDressSurfaceView>, PetDressController, PetDressController.Factory>();

            Container.BindInterfacesAndSelfTo<RoomInventorySelectionState>()
                .AsSingle();

            Container.Bind<ICoroutineService>()
                .FromMethod(_ => composition.CreateCoroutineService())
                .AsSingle()
                .NonLazy();

            Container.Bind<ScreenBlocker>()
                .FromMethod(_ => composition.CreateScreenBlocker())
                .AsSingle()
                .NonLazy();
        }

        private void BindGameData(GameProjectComposition composition)
        {
            Container.Bind<Data>()
                .FromMethod(ctx => composition.CreateGameData(ctx.Container.Resolve<IDataStorage>()))
                .AsSingle();

            Container.Bind<DataRepository>()
                .FromMethod(ctx =>
                    composition.CreateDataRepository(
                        ctx.Container.Resolve<Data>(),
                        ctx.Container.Resolve<EventDispatcher>(),
                        ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.BindInterfacesAndSelfTo<DataController>()
                .FromMethod(ctx =>
                    composition.CreateDataController(
                        ctx.Container.Resolve<DataRepository>(),
                        ctx.Container.Resolve<EventDispatcher>()))
                .AsSingle()
                .NonLazy();
        }

        private void BindNavigation(GameProjectComposition composition)
        {
            Container.Bind<INavigationService>()
                .FromMethod(ctx => composition.CreateNavigationService(ctx.Container.Resolve<EventDispatcher>()))
                .AsSingle();

            Container.Bind<IGameNavigationService>()
                .FromMethod(ctx =>
                    composition.CreateGameNavigationService(
                        ctx.Container.Resolve<INavigationService>(),
                        ctx.Container.Resolve<ICoroutineService>(),
                        ctx.Container.Resolve<EventDispatcher>(),
                        ctx.Container.Resolve<ScreenBlocker>()))
                .AsSingle()
                .NonLazy();
        }

        private void BindAudio(GameProjectComposition composition)
        {
            Container.Bind<AudioPlayer<SoundId>>()
                .FromMethod(ctx => composition.CreateAudioPlayer(ctx.Container.Resolve<ICoroutineService>()))
                .AsSingle();

            Container.Bind<AudioReactor>()
                .FromMethod(ctx =>
                    composition.CreateAudioReactor(
                        ctx.Container.Resolve<EventDispatcher>(),
                        ctx.Container.Resolve<AudioPlayer<SoundId>>()))
                .AsSingle()
                .NonLazy();
        }

        private void BindDevelopmentServices()
        {
            Container.Bind<PersistentGameDataService>()
                .AsSingle();

            Container.BindInterfacesTo<PersistentGameDataLifetime>()
                .AsSingle()
                .NonLazy();

            Container.BindInterfacesTo<DevelopmentProfileBootstrap>()
                .AsSingle()
                .NonLazy();
        }

        private static SceneSettings LoadSceneSettings()
        {
            SceneSettings sceneSettings = UnityEngine.Resources.Load<SceneSettings>(SceneSettingsResourcePath);

            if (sceneSettings == null)
            {
                throw new InvalidOperationException(
                    $"Could not load {nameof(SceneSettings)} from Resources/{SceneSettingsResourcePath}.");
            }

            return sceneSettings;
        }

        private static GameSoundLibrary LoadGameSoundLibrary()
        {
            GameSoundLibrary gameSoundLibrary =
                UnityEngine.Resources.Load<GameSoundLibrary>(GameSoundLibraryResourcePath);

            if (gameSoundLibrary == null)
            {
                throw new InvalidOperationException(
                    $"Could not load {nameof(GameSoundLibrary)} from Resources/{GameSoundLibraryResourcePath}.");
            }

            return gameSoundLibrary;
        }

        private static DevelopmentProfileSettings LoadDevelopmentProfileSettings()
        {
            return UnityEngine.Resources.Load<DevelopmentProfileSettings>(DevelopmentProfileSettingsResourcePath);
        }

        private static RoomSettings LoadRoomSettings()
        {
            RoomSettings roomSettings = UnityEngine.Resources.Load<RoomSettings>(RoomSettingsResourcePath);
            return roomSettings ?? UnityEngine.ScriptableObject.CreateInstance<RoomSettings>();
        }
    }
}
