# Marmilo / Brush Architecture Guide

This repository currently contains two main pieces:

- The Unity client app in `/Assets`
- The backend in `/Marmilo.Backend`

This document focuses on the Unity-side architecture, the communication patterns it uses, and the places where the ongoing migration to server-backed state is already visible.

For backend details, see [Marmilo.Backend/README.md](/Volumes/DiegoMac2/Diego/projects/Brush/Marmilo.Backend/README.md).

## Big Picture

At a high level, the client is organized around:

- `SceneBase`-driven screens
- Zenject composition and dependency injection
- An event-driven interaction model using `EventDispatcher`
- Thin controllers and view components in Unity
- Data/state services in `Game.Core`
- Reusable infrastructure in `Utilities`
- A new remote communication stack for Marmilo backend access

```mermaid
flowchart LR
    SCENES["SceneBase screens"] --> CTRL["Scene / room controllers"]
    CTRL --> CORE["Game.Core services and state"]
    CTRL --> VIEWS["Unity views and visual controllers"]
    CORE --> EVENTS["EventDispatcher"]
    VIEWS --> EVENTS
    CORE --> ASSETS["IAssetLoader"]
    CORE --> REMOTE["Remote communication stack"]
    REMOTE --> EXT["Supabase + Marmilo.Backend"]
    INSTALL["Zenject installers"] --> SCENES
    INSTALL --> CTRL
    INSTALL --> CORE
    INSTALL --> VIEWS
```

## Most Important Classes

These are the classes that currently define the shape of the client runtime:

- [GameProjectInstaller.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Installers/GameProjectInstaller.cs): global Zenject bindings
- [GameProjectComposition.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Installers/GameProjectComposition.cs): DI-agnostic object creation and bootstrap helpers
- [SceneBase.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Scenes/SceneBase.cs): base class for navigable screens
- [GameNavigationService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Scenes/GameNavigationService.cs): game-facing navigation orchestration
- [NavigationService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/Navigation/Core/NavigationService.cs): generic scene/popup navigator
- [EventDispatcher.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Core/Events/EventDispatcher.cs): event bus
- [DataRepository.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/DataRepository.cs): transitional gameplay state facade
- [ClientGameStateStore.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/ClientGameStateStore.cs): local client-side UI cache/store
- [DataController.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/DataController/DataController.cs): translates room events into repository writes
- [IAssetLoader.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/AssetLoader/IAssetLoader.cs) and [AddressablesAssetLoader.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/AssetLoader/AddressablesAssetLoader.cs): content loading abstraction
- [RemoteRequestDispatcher.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/RemoteCommunication/RemoteRequestDispatcher.cs): generic HTTP transport
- [MarmiloAuthService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/MarmiloAuthService.cs): auth/session orchestration
- [RemoteProfilesService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/RemoteProfilesService.cs): first server-backed gameplay slice in the client

### Runtime Class Diagram

```mermaid
classDiagram
    class GameProjectInstaller
    class GameProjectComposition
    class SceneBase
    class GameNavigationService
    class NavigationService
    class EventDispatcher
    class DataRepository
    class ClientGameStateStore
    class DataController
    class IAssetLoader
    class AddressablesAssetLoader
    class IRemoteRequestDispatcher
    class RemoteRequestDispatcher
    class IMarmiloAuthService
    class MarmiloAuthService
    class IProfilesService
    class RemoteProfilesService

    GameProjectInstaller --> GameProjectComposition
    GameProjectInstaller --> EventDispatcher
    GameProjectInstaller --> DataRepository
    GameProjectInstaller --> ClientGameStateStore
    GameProjectInstaller --> GameNavigationService
    GameProjectInstaller --> IAssetLoader
    GameProjectInstaller --> IRemoteRequestDispatcher
    GameProjectInstaller --> IMarmiloAuthService
    GameProjectInstaller --> IProfilesService

    SceneBase --> GameNavigationService
    SceneBase --> EventDispatcher

    GameNavigationService --> NavigationService
    GameNavigationService --> EventDispatcher

    DataController --> EventDispatcher
    DataController --> DataRepository

    DataRepository --> ClientGameStateStore

    AddressablesAssetLoader ..|> IAssetLoader
    RemoteRequestDispatcher ..|> IRemoteRequestDispatcher
    MarmiloAuthService ..|> IMarmiloAuthService
    RemoteProfilesService ..|> IProfilesService
    RemoteProfilesService --> ClientGameStateStore
    RemoteProfilesService --> IMarmiloAuthService
    RemoteProfilesService --> IRemoteRequestDispatcher
```

## Folder Map

- `/Assets/Scripts/Game/Core`
  - game state, rules, service interfaces, app services
- `/Assets/Scripts/Game/Unity`
  - scenes, MonoBehaviours, installers, scene controllers, view controllers
- `/Assets/Scripts/Utilities/Core`
  - pure infrastructure abstractions like events and remote communication models
- `/Assets/Scripts/Utilities/Unity`
  - Unity-specific implementations for asset loading, navigation, drag-and-drop, instantiation, UI
- `/Marmilo.Backend`
  - ASP.NET Core backend and tests

## 1. Server Communication

The project is moving toward a thin-client model where Unity owns UI/UX and the server owns business logic.

The first implemented vertical is `profiles`.

### Main Classes

- [RemoteRequest.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Core/RemoteCommunication/RemoteRequest.cs)
- [RemoteResponse.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Core/RemoteCommunication/RemoteResponse.cs)
- [IRemoteRequestDispatcher.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Core/RemoteCommunication/IRemoteRequestDispatcher.cs)
- [RemoteRequestDispatcher.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/RemoteCommunication/RemoteRequestDispatcher.cs)
- [SupabaseAuthApiClient.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/SupabaseAuthApiClient.cs)
- [MarmiloBackendApiClient.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/MarmiloBackendApiClient.cs)
- [MarmiloChildrenApiClient.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/MarmiloChildrenApiClient.cs)
- [MarmiloAuthService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/MarmiloAuthService.cs)
- [RemoteProfilesService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/RemoteProfilesService.cs)

### Class Diagram

```mermaid
classDiagram
    class RemoteRequest
    class RemoteResponse
    class IRemoteRequestDispatcher
    class RemoteRequestDispatcher
    class IMarmiloAuthService
    class MarmiloAuthService
    class IRemoteIdentityProviderClient
    class SupabaseAuthApiClient
    class IParentAccountApiClient
    class MarmiloBackendApiClient
    class IChildrenApiClient
    class MarmiloChildrenApiClient
    class IProfilesService
    class RemoteProfilesService
    class ClientGameStateStore

    RemoteRequestDispatcher ..|> IRemoteRequestDispatcher
    SupabaseAuthApiClient ..|> IRemoteIdentityProviderClient
    MarmiloBackendApiClient ..|> IParentAccountApiClient
    MarmiloChildrenApiClient ..|> IChildrenApiClient
    MarmiloAuthService ..|> IMarmiloAuthService
    RemoteProfilesService ..|> IProfilesService

    SupabaseAuthApiClient --> IRemoteRequestDispatcher
    MarmiloBackendApiClient --> IRemoteRequestDispatcher
    MarmiloChildrenApiClient --> IRemoteRequestDispatcher
    MarmiloAuthService --> IRemoteIdentityProviderClient
    MarmiloAuthService --> IParentAccountApiClient
    RemoteProfilesService --> IChildrenApiClient
    RemoteProfilesService --> IMarmiloAuthService
    RemoteProfilesService --> ClientGameStateStore
```

### Sequence: Account Creation and Session Persistence

```mermaid
sequenceDiagram
    participant UI as Auth UI
    participant Auth as MarmiloAuthService
    participant IdP as SupabaseAuthApiClient
    participant Dispatch as RemoteRequestDispatcher
    participant Supabase as Supabase Auth
    participant Api as MarmiloBackendApiClient
    participant Backend as Marmilo.Backend
    participant Storage as IDataStorage

    UI->>Auth: CreateAccountAsync(email, password, familyName)
    Auth->>IdP: SignUpAsync(email, password)
    IdP->>Dispatch: SendAsync(RemoteRequest)
    Dispatch->>Supabase: POST /auth/v1/signup
    Supabase-->>Dispatch: auth response
    Dispatch-->>IdP: RemoteResponse
    IdP-->>Auth: SupabaseAuthResponse
    Auth->>Api: RegisterParentAsync(accessToken, familyName)
    Api->>Dispatch: SendAsync(RemoteRequest)
    Dispatch->>Backend: POST /auth/register
    Backend-->>Dispatch: register response
    Dispatch-->>Api: RemoteResponse
    Api-->>Auth: MarmiloBackendRegisterResponse
    Auth->>Storage: SaveAsync(marmilo_auth_session, session)
    Auth-->>UI: MarmiloAuthResult.Success
```

### Sequence: Profiles Refresh / Create / Delete

```mermaid
sequenceDiagram
    participant Scene as Profile Scene
    participant Profiles as RemoteProfilesService
    participant Auth as IMarmiloAuthService
    participant Children as IChildrenApiClient
    participant Dispatch as RemoteRequestDispatcher
    participant Backend as Marmilo.Backend
    participant Store as ClientGameStateStore
    participant Bus as EventDispatcher

    Scene->>Profiles: RefreshAsync() / CreateAsync() / DeleteAsync()
    Profiles->>Auth: HasSession
    alt No session
        Profiles-->>Scene: return without mutating store
    else Has session
        Profiles->>Children: ListAsync() / CreateAsync() / DeleteAsync()
        Children->>Dispatch: SendAsync(RemoteRequest)
        Dispatch->>Backend: /children request
        Backend-->>Dispatch: response
        Dispatch-->>Children: RemoteResponse
        Children-->>Profiles: Profile data / success
        Profiles->>Store: update local cache
        Profiles->>Bus: ProfileUpdatedEvent
        Profiles->>Bus: ProfileSwitchedEvent (if needed)
        Profiles->>Bus: LocalDataChangedEvent
        Bus-->>Scene: refresh UI
    end
```

### Notes

- `RemoteRequestDispatcher` is intentionally generic and reusable.
- It handles:
  - request marshalling
  - bounded concurrency
  - retries through `IRemoteRetryPolicy`
  - HTTP transport using `UnityWebRequest`
- Game logic should not know about `UnityWebRequest`.
- The current migration keeps `DataRepository` alive, but new server-backed slices should ideally go through app services like `RemoteProfilesService`.

## 2. Controller-View Pattern

The Unity app mostly uses a pragmatic controller-view split:

- Scene entry points inherit from `SceneBase`
- Scene controllers orchestrate initialization and flows
- View components hold serialized references and visual behavior
- Some scenes split "logic controller" and "view controller" explicitly

### Main Classes

- [RoomSceneController.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/Controllers/RoomSceneController.cs)
- [RoomController.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/Controllers/RoomController.cs)
- [RoomViewController.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/Controllers/RoomViewController.cs)
- [RoomInventoryView.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/Inventory/RoomInventoryView.cs)
- [ProfileSelectionScene.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/ProfileSelectionScene/ProfileSelectionScene.cs)
- [ProfileManagementScene.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/ProfileManagementScene/ProfileManagementScene.cs)

### Class Diagram

```mermaid
classDiagram
    class SceneBase
    class RoomSceneController
    class RoomController
    class RoomViewController
    class RoomInventoryView
    class DataRepository
    class EventDispatcher
    class RoomPlaceableObjectsController
    class RoomPaintController

    RoomSceneController --|> SceneBase
    RoomSceneController --> RoomController
    RoomSceneController --> RoomViewController

    RoomController --> DataRepository
    RoomController --> EventDispatcher
    RoomController --> RoomInventoryView

    RoomViewController --> DataRepository
    RoomViewController --> EventDispatcher
    RoomViewController --> RoomPlaceableObjectsController
    RoomViewController --> RoomPaintController
```

### Sequence: Room Scene Initialization

```mermaid
sequenceDiagram
    participant Scene as RoomSceneController
    participant Logic as RoomController
    participant View as RoomViewController
    participant Repo as DataRepository
    participant Visuals as Visual sub-controllers

    Scene->>Logic: Initialize()
    Logic->>Logic: Refresh inventory list
    Logic->>Logic: Refresh drop areas
    Logic->>Logic: Refresh pet name input
    Scene->>View: Initialize()
    View->>Visuals: Create room/pet/paint controllers
    View->>Repo: read current persisted state
    Repo-->>View: room/pet/inventory state
    View->>Visuals: Refresh visuals from data
```

### Practical Pattern

- `RoomController` handles input, UI coordination, navigation, and dispatching gameplay intent.
- `RoomViewController` applies already-persisted state to visual surfaces.
- `DataController` listens to events and writes mutations into `DataRepository`.

That gives a useful separation:

- Interaction -> `RoomController`
- Mutation bridge -> `DataController`
- Visual refresh -> `RoomViewController`

## 3. Asset Loading

The good architectural decision here is already in place: the game depends on `IAssetLoader`, not directly on Addressables.

### Main Classes

- [IAssetLoader.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/AssetLoader/IAssetLoader.cs)
- [AddressablesAssetLoader.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/AssetLoader/AddressablesAssetLoader.cs)
- [IAssetLoadHandle.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/AssetLoader/IAssetLoadHandle.cs)
- [AddressablesAssetLoadHandle.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/AssetLoader/AddressablesAssetLoadHandle.cs)
- [ItemViewRenderer.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/ItemViews/ItemViewRenderer.cs)

### Class Diagram

```mermaid
classDiagram
    class IAssetLoader
    class AddressablesAssetLoader
    class IAssetLoadHandle~T~
    class AddressablesAssetLoadHandle~T~
    class ItemViewRenderer

    AddressablesAssetLoader ..|> IAssetLoader
    AddressablesAssetLoadHandle~T~ ..|> IAssetLoadHandle~T~
    ItemViewRenderer --> IAssetLoader
    ItemViewRenderer --> IAssetLoadHandle~Sprite~
```

### Sequence: Load Item Sprite

```mermaid
sequenceDiagram
    participant Renderer as ItemViewRenderer
    participant Loader as IAssetLoader
    participant Addressables as AddressablesAssetLoader
    participant Handle as AddressablesAssetLoadHandle

    Renderer->>Loader: LoadAssetAsync<Sprite>(key)
    Loader->>Addressables: LoadAssetAsync<Sprite>(key)
    Addressables-->>Handle: async handle
    Handle-->>Renderer: completion callback(sprite)
    Renderer->>Renderer: release previous handle
    Renderer->>Renderer: update image state
```

### Why This Matters

- The game layer does not know about `Addressables`.
- Missing/released asset handling is centralized in `ItemViewRenderer`.
- Evolving from a simple `LoadAssetAsync` API to preload/release/catalog refresh later is incremental.

## 4. Scene Navigation

Navigation is two-layered:

- `NavigationService` is generic and works with scenes and prefab popups.
- `GameNavigationService` adds game-level rules like screen blocking, queuing, and scene-specific events.

### Main Classes

- [INavigationService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/Navigation/Abstractions/INavigationService.cs)
- [NavigationService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/Navigation/Core/NavigationService.cs)
- [IGameNavigationService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Scenes/IGameNavigationService.cs)
- [GameNavigationService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Scenes/GameNavigationService.cs)
- [SceneBase.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Scenes/SceneBase.cs)
- [SceneNavigationNodeResolver.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Scenes/SceneNavigationNodeResolver.cs)
- [SceneSettings.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Scenes/SceneSettings.cs)

### Class Diagram

```mermaid
classDiagram
    class SceneBase
    class IGameNavigationService
    class GameNavigationService
    class INavigationService
    class NavigationService
    class SceneSettings
    class EventDispatcher

    GameNavigationService ..|> IGameNavigationService
    NavigationService ..|> INavigationService
    SceneBase --> IGameNavigationService
    GameNavigationService --> INavigationService
    GameNavigationService --> SceneSettings
    GameNavigationService --> EventDispatcher
    NavigationService --> EventDispatcher
```

### Sequence: Navigate to a Scene

```mermaid
sequenceDiagram
    participant Caller as Scene / UI
    participant GameNav as GameNavigationService
    participant Nav as NavigationService
    participant Bus as EventDispatcher
    participant Blocker as ScreenBlocker

    Caller->>GameNav: Navigate(SceneType, params)
    alt Transition already in progress
        GameNav->>GameNav: enqueue request
    else Ready
        GameNav->>Nav: Navigate(target, params)
        Nav->>Bus: NavigationTransitionStartedEvent
        Bus-->>GameNav: OnNavigationTransitionStarted
        GameNav->>Blocker: BlockScope("Navigation")
        Nav->>Nav: resolve node and run transition strategy
        Nav->>Bus: NavigationTransitionFinishedEvent
        Bus-->>GameNav: OnNavigationTransitionFinished
        GameNav->>Blocker: dispose block scope
        GameNav->>GameNav: process queued request if any
    end
```

### Notes

- `SceneBase` gets `EventDispatcher` and `IGameNavigationService` injected.
- `NavigationService` can navigate both full scenes and popup prefabs.
- `GameNavigationService` serializes scene navigation requests and prevents overlapping transitions.

## 5. Zenject and Instantiators

There are two separate ideas here:

- Zenject composes the runtime graph.
- Instantiators decide whether a prefab is created with plain Unity or through Zenject injection.

### Main Classes

- [GameProjectInstaller.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Installers/GameProjectInstaller.cs)
- [GameProjectComposition.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Installers/GameProjectComposition.cs)
- [IObjectInstantiator.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/Instantiator/IObjectInstantiator.cs)
- [UnityInstantiator.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/Instantiator/UnityInstantiator.cs)
- [DependencyObjectInstantiator.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Instantiator/DependencyObjectInstantiator.cs)
- [InstantiatorIds.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/Definitions/InstantiatorIds.cs)

### Class Diagram

```mermaid
classDiagram
    class GameProjectInstaller
    class GameProjectComposition
    class IObjectInstantiator
    class UnityInstantiator
    class DependencyObjectInstantiator
    class DataRepository
    class EventDispatcher
    class RemoteProfilesService

    UnityInstantiator ..|> IObjectInstantiator
    DependencyObjectInstantiator ..|> IObjectInstantiator

    GameProjectInstaller --> GameProjectComposition
    GameProjectInstaller --> UnityInstantiator
    GameProjectInstaller --> DependencyObjectInstantiator
    GameProjectInstaller --> DataRepository
    GameProjectInstaller --> EventDispatcher
    GameProjectInstaller --> RemoteProfilesService
```

### Sequence: Application Bootstrap

```mermaid
sequenceDiagram
    participant Unity as Unity runtime
    participant Installer as GameProjectInstaller
    participant Composition as GameProjectComposition
    participant Container as Zenject Container
    participant Services as Runtime services

    Unity->>Installer: InstallBindings()
    Installer->>Composition: create composition helper
    Installer->>Composition: load/create settings and factories
    Installer->>Container: Bind instances and interfaces
    Installer->>Container: Bind non-lazy services
    Container-->>Services: instantiate dispatcher/navigation/audio/etc.
    Services-->>Unity: runtime graph ready
```

### Why Two Instantiators?

- `UnityInstantiator` is for plain prefab creation.
- `DependencyObjectInstantiator` routes prefab creation through Zenject, so the created prefab gets injection.

This is especially useful for visual objects that need services or factories after instantiation.

## 6. Drag and Drop

Drag-and-drop is built as reusable infrastructure in `Utilities`, then specialized in the room scene.

### Main Classes

- [UIDraggable.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/DragAndDrop/UIDraggable.cs)
- [UIDropTarget.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/DragAndDrop/UIDropTarget.cs)
- [UIDragScrollRouter.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Unity/DragAndDrop/UIDragScrollRouter.cs)
- [RoomInventoryDraggable.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/DragItems/RoomInventoryDraggable.cs)
- [RoomPlacedObjectDraggable.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/DragItems/RoomPlacedObjectDraggable.cs)
- [RoomDropArea.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/DragItems/RoomDropArea.cs)
- [RoomInventoryDragData.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/DragItems/RoomInventoryDragData.cs)

### Class Diagram

```mermaid
classDiagram
    class UIDraggable
    class UIDropTarget
    class RoomInventoryDraggable
    class RoomPlacedObjectDraggable
    class RoomDropArea
    class EventDispatcher
    class DataRepository

    RoomInventoryDraggable --|> UIDraggable
    RoomPlacedObjectDraggable --|> UIDraggable
    RoomDropArea --|> UIDropTarget
    RoomDropArea --> EventDispatcher
    RoomDropArea --> DataRepository
```

### Sequence: Drag Inventory Item into Room

```mermaid
sequenceDiagram
    participant Drag as UIDraggable
    participant Drop as RoomDropArea
    participant Room as RoomController
    participant Bus as EventDispatcher
    participant DataCtrl as DataController
    participant Repo as DataRepository
    participant View as RoomViewController

    Drag->>Bus: RoomInventoryDragStartedEvent
    Bus-->>Drop: show eligible targets
    Drag->>Drop: hover / drop
    Drop->>Drop: CanAccept(draggable)
    Drop->>Bus: RoomInventoryDropAcceptedEvent
    Bus-->>Room: OnRoomInventoryDropAccepted
    Room->>Bus: RoomObjectPlacedEvent / RoomPaintAppliedEvent / PetDataAppliedEvent
    Bus-->>DataCtrl: matching handler
    DataCtrl->>Repo: mutate persisted state
    Repo->>Bus: InventoryUpdatedEvent / RoomDataItemAppliedEvent / PetDataAppliedEvent
    Bus-->>View: refresh visuals
```

### Design Observation

This is a strong pattern in the project:

- generic infrastructure in `Utilities`
- game-specific policy in `Game.Unity`

## 7. EventDispatcher

`EventDispatcher` is one of the central architectural pieces of the client.

It is used for:

- gameplay mutation requests
- visual refresh notifications
- navigation transition notifications
- drag-and-drop lifecycle
- room inventory UX

### Main Classes

- [EventDispatcher.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Utilities/Core/Events/EventDispatcher.cs)
- [DataController.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/DataController/DataController.cs)
- [RoomController.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/Controllers/RoomController.cs)
- [RoomViewController.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Unity/RoomScene/Controllers/RoomViewController.cs)

### Class Diagram

```mermaid
classDiagram
    class EventDispatcher
    class RoomController
    class DataController
    class DataRepository
    class RoomViewController
    class GameNavigationService
    class NavigationService

    RoomController --> EventDispatcher
    DataController --> EventDispatcher
    DataController --> DataRepository
    RoomViewController --> EventDispatcher
    GameNavigationService --> EventDispatcher
    NavigationService --> EventDispatcher
```

### Sequence: Event-Driven Mutation Loop

```mermaid
sequenceDiagram
    participant User as User
    participant Controller as Scene / room controller
    participant Bus as EventDispatcher
    participant DataCtrl as DataController
    participant Repo as DataRepository
    participant Views as Views / visual controllers

    User->>Controller: interaction
    Controller->>Bus: send gameplay event
    Bus-->>DataCtrl: deliver event
    DataCtrl->>Repo: mutate state
    Repo->>Bus: emit updated events
    Bus-->>Views: refresh notifications
```

### Sender-Specific Events

`EventDispatcher` supports:

- global subscriptions by event type
- sender-specific subscriptions by `(event type, sender)`

That makes it flexible enough for broad app-level events and also for more local coordination.

## 8. Data and State Today

The current state model is transitional.

### Main Classes

- [Data.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Data/Data.cs)
- [ClientGameStateStore.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/ClientGameStateStore.cs)
- [DataRepository.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/DataRepository.cs)
- [IProfileService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/IProfileService.cs)
- [LocalProfileService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/LocalProfileService.cs)
- [IProfilesService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/IProfilesService.cs)
- [RemoteProfilesService.cs](/Volumes/DiegoMac2/Diego/projects/Brush/Assets/Scripts/Game/Core/Services/RemoteProfilesService.cs)

### Class Diagram

```mermaid
classDiagram
    class Data
    class Profile
    class ClientGameStateStore
    class DataRepository
    class IProfileService
    class LocalProfileService
    class IProfilesService
    class RemoteProfilesService

    Data --> Profile
    ClientGameStateStore --> Data
    DataRepository --> ClientGameStateStore
    LocalProfileService ..|> IProfileService
    DataRepository --> IProfileService
    RemoteProfilesService ..|> IProfilesService
    RemoteProfilesService --> ClientGameStateStore
```

### Current Direction

- `DataRepository` is no longer the ideal final shape.
- `ClientGameStateStore` is the more accurate concept for local client state.
- `IProfileService` is still a local transitional service used by `DataRepository`.
- `IProfilesService` is the server-backed path now used by profile scenes.

This is the pattern to repeat for other areas:

- `economy`
- `market`
- `child_game_state`

## 9. Recommended Mental Model

When working in this repo, this is the safest mental model:

- `Utilities` contains reusable infrastructure
- `Game.Core` contains app/domain-side state and orchestration
- `Game.Unity` contains Unity-specific presentation and scene behavior
- `Marmilo.Backend` is becoming the source of truth for business logic

### Final Direction

```mermaid
flowchart LR
    UI["Unity UI / UX"] --> APP["Client app services"]
    APP --> CACHE["ClientGameStateStore"]
    APP --> SDK["Remote domain clients"]
    SDK --> TRANSPORT["RemoteRequestDispatcher"]
    TRANSPORT --> SERVER["Marmilo.Backend"]
    SERVER --> DB["Postgres"]
```

In that future shape:

- Unity owns presentation
- the backend owns rules and persistence
- local client data becomes cache/state for UX, not source of truth

## 10. What Is Still in Transition

This is important so the current architecture is interpreted correctly:

- `profiles` already have a server-backed client slice
- many other gameplay mutations still go through local `DataRepository`
- `DataController` still translates many scene events into local repository mutations
- `brushSessionDurationMinutes` is still local in the client
- room/pet/inventory visual state is still applied from local client state

So the codebase is not yet "fully server-first", but the architectural direction is already clear and the first vertical is in place.
