using System;

using Flowbit.Utilities.Audio;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.Coroutines;
using Flowbit.Utilities.Navigation;
using Flowbit.Utilities.RemoteCommunication;
using Flowbit.Utilities.Storage;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.Unity.AssetLoader;
using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.Logger;
using Flowbit.Utilities.Unity.RemoteCommunication;
using Flowbit.Utilities.Unity.UI;

using Game.Core.DataController;
using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Audio;
using Game.Unity.Development;
using Game.Unity.Definitions;
using Game.Unity.Instantiator;
using Game.Unity.RoomScene;
using Game.Unity.Scenes;
using Game.Unity.Settings;
using Game.Unity.UI;

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
        private const string DevelopmentBootstrapSettingsResourcePath = "DevelopmentBootstrapSettings";
        private const string RoomSettingsResourcePath = "RoomSettings";
        private const string UISettingsResourcePath = "UISettings";

        public override void InstallBindings()
        {
            SceneSettings sceneSettings = LoadSceneSettings();
            GameSoundLibrary gameSoundLibrary = LoadGameSoundLibrary();
            DevelopmentBootstrapSettings developmentBootstrapSettings = LoadDevelopmentBootstrapSettings();
            RoomSettings roomSettings = LoadRoomSettings();
            UISettings uiSettings = LoadUISettings();
            GameProjectComposition composition = new GameProjectComposition(sceneSettings, gameSoundLibrary);

            composition.PreventScreenSleep();

            Container.BindInstance(composition).AsSingle();
            Container.BindInstance(sceneSettings).AsSingle();
            Container.BindInstance(gameSoundLibrary).AsSingle();
            Container.BindInstance(developmentBootstrapSettings).AsSingle();
            Container.BindInstance(roomSettings).AsSingle();
            Container.BindInstance(uiSettings).AsSingle();

            BindCoreServices(composition, uiSettings);
            BindGameData(composition);
            BindNavigation(composition);
            BindAudio(composition);
            BindDevelopmentServices();
        }

        private void BindCoreServices(GameProjectComposition composition, UISettings uiSettings)
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

            Container.Bind<BackendSettings>()
                .FromMethod(_ => composition.CreateBackendSettings())
                .AsSingle();

            Container.Bind<EventDispatcher>()
                .FromMethod(_ => composition.CreateEventDispatcher())
                .AsSingle();

            Container.Bind<IGameLogger>()
                .To<UnityGameLogger>()
                .AsSingle();

            Container.Bind<ILocalizationService>()
                .FromMethod(_ =>
                {
                    LocalizationService service = new LocalizationService();
                    LocalizationServiceLocator.Current = service;
                    return service;
                })
                .AsSingle();

            Container.Bind<IRemotePayloadCodec>()
                .To<JsonUtilityRemotePayloadCodec>()
                .AsSingle();

            Container.Bind<IRemoteRetryPolicy>()
                .To<DefaultRemoteRetryPolicy>()
                .AsSingle();

            Container.Bind<IRemoteRequestDispatcher>()
                .To<RemoteRequestDispatcher>()
                .AsSingle();

            Container.Bind<IRemoteIdentityProviderClient>()
                .To<SupabaseAuthApiClient>()
                .AsSingle();

            Container.Bind<IParentAccountApiClient>()
                .To<BackendApiClient>()
                .AsSingle();

            Container.Bind<IChildrenApiClient>()
                .To<ChildrenApiClient>()
                .AsSingle();

            Container.Bind<IAuthService>()
                .To<AuthService>()
                .AsSingle()
                .NonLazy();

            Container.BindInterfacesTo<AuthLifetime>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IProfilesService>()
                .To<RemoteProfilesService>()
                .AsSingle();

            Container.Bind<IRewardClaimService>()
                .To<RewardClaimService>()
                .AsSingle();

            Container.Bind<IMarketPurchaseService>()
                .To<MarketPurchaseService>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<ChildGameStateSyncService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<IAssetLoader>()
                .To<AddressablesAssetLoader>()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<InventoryDropEffectPositionTracker>()
                .AsSingle();

            Container.Bind<DefaultItemView>()
                .AsSingle();

            Container.Bind<IItemView>()
                .To<PlaceableObjectItemView>()
                .AsSingle();

            Container.Bind<IItemView>()
                .To<FoodItemView>()
                .AsSingle();

            Container.Bind<IItemView>()
                .To<PaintItemView>()
                .AsSingle();

            Container.Bind<IItemView>()
                .To<SkinItemView>()
                .AsSingle();

            Container.Bind<IItemView>()
                .To<DressItemView>()
                .AsSingle();

            Container.Bind<IItemView>()
                .To<EyesItemView>()
                .AsSingle();

            Container.Bind<IItemView>()
                .To<CurrencyItemView>()
                .AsSingle();

            Container.Bind<ItemViewRegistry>()
                .AsSingle();

            if (uiSettings.AnimatedComponentControllerPrefab == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(UISettings)} requires an {nameof(UIAnimatedComponentController)} prefab reference.");
            }

            Container.Bind<UIAnimatedComponentController>()
                .FromComponentInNewPrefab(uiSettings.AnimatedComponentControllerPrefab)
                .AsSingle()
                .NonLazy();

            Container.BindFactory<System.Collections.Generic.IReadOnlyList<PetEyesSurfaceView>, PetEyesController, PetEyesController.Factory>();
            Container.BindFactory<System.Collections.Generic.IReadOnlyList<PetHatSurfaceView>, PetHatController, PetHatController.Factory>();
            Container.BindFactory<System.Collections.Generic.IReadOnlyList<PetSkinSurfaceView>, PetSkinController, PetSkinController.Factory>();
            Container.BindFactory<System.Collections.Generic.IReadOnlyList<PetDressSurfaceView>, PetDressController, PetDressController.Factory>();
            Container.BindFactory<PetView, PetFoodController, PetFoodController.Factory>();

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

            Container.Bind<ClientGameStateStore>()
                .FromMethod(ctx => composition.CreateClientGameStateStore(ctx.Container.Resolve<Data>()))
                .AsSingle();

            Container.Bind<IProfileService>()
                .FromMethod(ctx =>
                    composition.CreateProfileService(
                        ctx.Container.Resolve<ClientGameStateStore>(),
                        ctx.Container.Resolve<EventDispatcher>()))
                .AsSingle();

            Container.Bind<IRoomGameplayService>()
                .FromMethod(ctx =>
                    composition.CreateRoomGameplayService(
                        ctx.Container.Resolve<ClientGameStateStore>(),
                        ctx.Container.Resolve<EventDispatcher>(),
                        ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.Bind<DataRepository>()
                .FromMethod(ctx =>
                    composition.CreateDataRepository(
                        ctx.Container.Resolve<ClientGameStateStore>(),
                        ctx.Container.Resolve<IProfileService>(),
                        ctx.Container.Resolve<IRoomGameplayService>(),
                        ctx.Container.Resolve<EventDispatcher>()))
                .AsSingle();

            Container.BindInterfacesAndSelfTo<DataController>()
                .FromMethod(ctx =>
                    composition.CreateDataController(
                        ctx.Container.Resolve<IRoomGameplayService>(),
                        ctx.Container.Resolve<EventDispatcher>()))
                .AsSingle()
                .NonLazy();
        }

        private void BindNavigation(GameProjectComposition composition)
        {
            Container.Bind<INavigationService>()
                .FromMethod(ctx =>
                    composition.CreateNavigationService(
                        ctx.Container.Resolve<EventDispatcher>(),
                        ctx.Container.ResolveId<IObjectInstantiator>(InstantiatorIds.Dependency)))
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

            Container.Bind<DevelopmentDebugPanel>()
                .FromNewComponentOnNewGameObject()
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

        private static DevelopmentBootstrapSettings LoadDevelopmentBootstrapSettings()
        {
            return UnityEngine.Resources.Load<DevelopmentBootstrapSettings>(DevelopmentBootstrapSettingsResourcePath);
        }

        private static RoomSettings LoadRoomSettings()
        {
            RoomSettings roomSettings = UnityEngine.Resources.Load<RoomSettings>(RoomSettingsResourcePath);
            return roomSettings ?? UnityEngine.ScriptableObject.CreateInstance<RoomSettings>();
        }

        private static UISettings LoadUISettings()
        {
            UISettings uiSettings = UnityEngine.Resources.Load<UISettings>(UISettingsResourcePath);

            if (uiSettings == null)
            {
                throw new InvalidOperationException(
                    $"Could not load {nameof(UISettings)} from Resources/{UISettingsResourcePath}.");
            }

            return uiSettings;
        }
    }
}
