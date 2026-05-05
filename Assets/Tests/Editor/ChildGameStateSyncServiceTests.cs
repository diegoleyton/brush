using System.Collections.Generic;
using System.Threading.Tasks;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.Storage;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;

using NUnit.Framework;

using GameState = Game.Core.Data.Data;

namespace Game.Core.Tests
{
    public sealed class ChildGameStateSyncServiceTests
    {
        [Test]
        public async Task ReloadCurrentProfileAsync_ReturnsFalseAndMarksOffline_WhenRemoteLoadFails()
        {
            TestContext context = CreateContext();
            context.ChildrenApiClient.GameStateByProfileId["child-1"] = null;
            ChildGameStateSyncService service = context.CreateService();
            service.Initialize();

            bool loaded = await service.ReloadCurrentProfileAsync();

            Assert.That(loaded, Is.False);
            Assert.That(service.IsOffline, Is.True);
            Assert.That(context.SyncStatusRecorder.LastIsOffline, Is.True);

            service.Dispose();
        }

        [Test]
        public async Task ReloadCurrentProfileAsync_FlushesMatchingPendingSyncAndClearsEnvelope_OnSuccessfulPush()
        {
            TestContext context = CreateContext();
            context.Storage.LoadEnvelope = new PendingChildGameStateSyncEnvelope
            {
                Entries = new List<PendingChildGameStateSyncEntry>
                {
                    new PendingChildGameStateSyncEntry
                    {
                        RemoteProfileId = "child-1",
                        BaseRevision = "rev-1",
                        Snapshot = new ChildGameStateSnapshot
                        {
                            ChildId = "child-1",
                            Revision = "rev-1",
                            CoinsBalance = 15,
                            PetState = new Pet { Name = "Local Pet" },
                            RoomState = new Room(),
                            InventoryState = new Inventory()
                        }
                    }
                }
            };
            context.ChildrenApiClient.GameStateByProfileId["child-1"] = new ChildGameStateSnapshot
            {
                ChildId = "child-1",
                Revision = "rev-1",
                CoinsBalance = 10,
                PetState = new Pet { Name = "Server Pet" },
                RoomState = new Room(),
                InventoryState = new Inventory()
            };
            context.ChildrenApiClient.PushResult = new ChildGameStateSyncPushResult
            {
                Status = ChildGameStateSyncPushStatus.Success,
                Snapshot = new ChildGameStateSnapshot
                {
                    ChildId = "child-1",
                    Revision = "rev-2",
                    CoinsBalance = 15,
                    PetState = new Pet { Name = "Local Pet" },
                    RoomState = new Room(),
                    InventoryState = new Inventory()
                }
            };

            ChildGameStateSyncService service = context.CreateService();
            service.Initialize();

            bool loaded = await service.ReloadCurrentProfileAsync();

            Assert.That(loaded, Is.True);
            Assert.That(service.IsOffline, Is.False);
            Assert.That(service.HasPendingSync, Is.False);
            Assert.That(context.ChildrenApiClient.PushCallCount, Is.EqualTo(1));
            Assert.That(context.ChildrenApiClient.LastPushRemoteProfileId, Is.EqualTo("child-1"));
            Assert.That(context.ChildrenApiClient.LastPushBaseRevision, Is.EqualTo("rev-1"));
            Assert.That(context.Store.CurrentProfile.RemoteGameStateRevision, Is.EqualTo("rev-2"));
            Assert.That(context.Store.CurrentProfile.Coins, Is.EqualTo(15));
            Assert.That(context.Store.CurrentProfile.PetData.Name, Is.EqualTo("Local Pet"));
            Assert.That(context.Storage.LastSavedEnvelope.Entries.Count, Is.EqualTo(0));

            service.Dispose();
        }

        [Test]
        public async Task LocalChange_PersistsPendingSyncAndMarksOffline_WhenPushHitsTransportFailure()
        {
            TestContext context = CreateContext();
            context.ChildrenApiClient.GameStateByProfileId["child-1"] = new ChildGameStateSnapshot
            {
                ChildId = "child-1",
                Revision = "rev-1",
                CoinsBalance = 10,
                BrushSessionDurationMinutes = 2,
                PetState = new Pet { Name = "Server Pet" },
                RoomState = new Room(),
                InventoryState = new Inventory()
            };
            context.ChildrenApiClient.PushResult = new ChildGameStateSyncPushResult
            {
                Status = ChildGameStateSyncPushStatus.TransportFailure
            };

            ChildGameStateSyncService service = context.CreateService();
            service.Initialize();
            await service.ReloadCurrentProfileAsync();

            context.Store.CurrentProfile.Coins = 25;
            context.Dispatcher.Send(new ChildGameStateLocallyChangedEvent());
            await Task.Delay(50);

            Assert.That(service.HasPendingSync, Is.True);
            Assert.That(service.IsOffline, Is.True);
            Assert.That(context.ChildrenApiClient.PushCallCount, Is.EqualTo(1));
            Assert.That(context.Storage.LastSavedEnvelope, Is.Not.Null);
            Assert.That(context.Storage.LastSavedEnvelope.Entries.Count, Is.EqualTo(1));
            Assert.That(context.Storage.LastSavedEnvelope.Entries[0].RemoteProfileId, Is.EqualTo("child-1"));
            Assert.That(context.Storage.LastSavedEnvelope.Entries[0].Snapshot.CoinsBalance, Is.EqualTo(25));
            Assert.That(context.SyncStatusRecorder.LastHasPendingSync, Is.True);
            Assert.That(context.SyncStatusRecorder.LastIsOffline, Is.True);

            service.Dispose();
        }

        [Test]
        public async Task LocalChange_ReplacesLocalStateAndPublishesFailure_WhenServerRejectsPush()
        {
            TestContext context = CreateContext();
            context.ChildrenApiClient.QueueGameState(
                "child-1",
                new ChildGameStateSnapshot
                {
                    ChildId = "child-1",
                    Revision = "rev-1",
                    CoinsBalance = 10,
                    BrushSessionDurationMinutes = 2,
                    PetState = new Pet { Name = "Server Pet" },
                    RoomState = new Room(),
                    InventoryState = new Inventory()
                },
                new ChildGameStateSnapshot
                {
                    ChildId = "child-1",
                    Revision = "rev-2",
                    CoinsBalance = 40,
                    BrushSessionDurationMinutes = 3,
                    PetState = new Pet { Name = "Server Wins" },
                    RoomState = new Room(),
                    InventoryState = new Inventory()
                });
            context.ChildrenApiClient.PushResult = new ChildGameStateSyncPushResult
            {
                Status = ChildGameStateSyncPushStatus.ServerRejected,
                ErrorMessage = "Rejected by server."
            };

            ChildGameStateSyncService service = context.CreateService();
            service.Initialize();
            await service.ReloadCurrentProfileAsync();

            context.Store.CurrentProfile.Coins = 999;
            context.Store.CurrentProfile.PetData.Name = "Local Change";
            context.Dispatcher.Send(new ChildGameStateLocallyChangedEvent());
            await Task.Delay(50);

            Assert.That(service.HasPendingSync, Is.False);
            Assert.That(service.IsOffline, Is.False);
            Assert.That(context.Store.CurrentProfile.Coins, Is.EqualTo(40));
            Assert.That(context.Store.CurrentProfile.PetData.Name, Is.EqualTo("Server Wins"));
            Assert.That(context.FailureRecorder.Messages.Count, Is.EqualTo(1));
            Assert.That(context.FailureRecorder.Messages[0], Is.EqualTo("Rejected by server."));

            service.Dispose();
        }

        private static TestContext CreateContext()
        {
            Profile profile = new Profile
            {
                RemoteProfileId = "child-1",
                RemoteGameStateRevision = "rev-1",
                Name = "Kid",
                PetData = new Pet { Name = "Initial Pet" },
                RoomData = new Room(),
                InventoryData = new Inventory()
            };

            GameState data = new GameState();
            data.Profiles.Add(profile);
            data.CurrentProfile = 0;

            EventDispatcher dispatcher = new EventDispatcher();
            ClientGameStateStore store = new ClientGameStateStore(data);
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient();
            FakeDataStorage storage = new FakeDataStorage();
            FakeGameLogger logger = new FakeGameLogger();
            FakeAuthService authService = new FakeAuthService(hasSession: true);

            return new TestContext(
                dispatcher,
                store,
                childrenApiClient,
                storage,
                logger,
                authService);
        }

        private sealed class TestContext
        {
            public TestContext(
                EventDispatcher dispatcher,
                ClientGameStateStore store,
                FakeChildrenApiClient childrenApiClient,
                FakeDataStorage storage,
                FakeGameLogger logger,
                FakeAuthService authService)
            {
                Dispatcher = dispatcher;
                Store = store;
                ChildrenApiClient = childrenApiClient;
                Storage = storage;
                Logger = logger;
                AuthService = authService;
                SyncStatusRecorder = new SyncStatusRecorder(dispatcher);
                FailureRecorder = new FailureRecorder(dispatcher);
            }

            public EventDispatcher Dispatcher { get; }

            public ClientGameStateStore Store { get; }

            public FakeChildrenApiClient ChildrenApiClient { get; }

            public FakeDataStorage Storage { get; }

            public FakeGameLogger Logger { get; }

            public FakeAuthService AuthService { get; }

            public SyncStatusRecorder SyncStatusRecorder { get; }

            public FailureRecorder FailureRecorder { get; }

            public ChildGameStateSyncService CreateService()
            {
                return new ChildGameStateSyncService(
                    Dispatcher,
                    Store,
                    ChildrenApiClient,
                    AuthService,
                    Storage,
                    Logger);
            }
        }

        private sealed class FakeAuthService : IAuthService
        {
            public FakeAuthService(bool hasSession)
            {
                HasSession = hasSession;
            }

            public AuthSession CurrentSession => null;

            public bool HasSession { get; }

            public Task InitializeAsync() => Task.CompletedTask;

            public Task<AuthResult> CreateAccountAsync(string email, string password, string familyName) =>
                Task.FromResult<AuthResult>(null);

            public Task<AuthResult> LoginAsync(string email, string password) =>
                Task.FromResult<AuthResult>(null);

            public Task LogoutAsync() => Task.CompletedTask;
        }

        private sealed class FakeChildrenApiClient : IChildrenApiClient
        {
            public Dictionary<string, ChildGameStateSnapshot> GameStateByProfileId { get; } = new();

            private readonly Dictionary<string, Queue<ChildGameStateSnapshot>> queuedGameStatesByProfileId_ = new();

            public ChildGameStateSyncPushResult PushResult { get; set; }

            public int PushCallCount { get; private set; }

            public string LastPushRemoteProfileId { get; private set; }

            public string LastPushBaseRevision { get; private set; }

            public Task<IReadOnlyList<Profile>> ListAsync() =>
                Task.FromResult<IReadOnlyList<Profile>>(null);

            public Task<Profile> CreateAsync(string name, string petName, int pictureId) =>
                Task.FromResult<Profile>(null);

            public Task<bool> DeleteAsync(string remoteProfileId) =>
                Task.FromResult(false);

            public Task<bool> UpdateProfileAsync(string remoteProfileId, string name, string petName, int pictureId, bool isActive) =>
                Task.FromResult(false);

            public Task<ChildGameStateSnapshot> GetGameStateAsync(string remoteProfileId)
            {
                if (queuedGameStatesByProfileId_.TryGetValue(remoteProfileId, out Queue<ChildGameStateSnapshot> queuedSnapshots) &&
                    queuedSnapshots.Count > 0)
                {
                    return Task.FromResult(queuedSnapshots.Dequeue());
                }

                GameStateByProfileId.TryGetValue(remoteProfileId, out ChildGameStateSnapshot snapshot);
                return Task.FromResult(snapshot);
            }

            public void QueueGameState(string remoteProfileId, params ChildGameStateSnapshot[] snapshots)
            {
                Queue<ChildGameStateSnapshot> queue = new Queue<ChildGameStateSnapshot>();
                if (snapshots != null)
                {
                    for (int index = 0; index < snapshots.Length; index++)
                    {
                        queue.Enqueue(snapshots[index]);
                    }
                }

                queuedGameStatesByProfileId_[remoteProfileId] = queue;
            }

            public Task<ChildGameStateSyncPushResult> PushGameStateAsync(string remoteProfileId, string baseRevision, ChildGameStateSnapshot snapshot)
            {
                PushCallCount++;
                LastPushRemoteProfileId = remoteProfileId;
                LastPushBaseRevision = baseRevision;
                return Task.FromResult(PushResult);
            }

            public Task<ChildGameStateSnapshot> CompleteBrushSessionAsync(string remoteProfileId) =>
                Task.FromResult<ChildGameStateSnapshot>(null);

            public Task<Reward[]> ClaimRewardsAsync(string remoteProfileId) =>
                Task.FromResult<Reward[]>(null);

            public Task<MarketPurchaseStatus> PurchaseMarketItemAsync(string remoteProfileId, MarketItemDefinition itemDefinition) =>
                Task.FromResult(MarketPurchaseStatus.ITEM_NOT_FOUND);

            public Task<bool> GrantCoinsAsync(string remoteProfileId, int amount, string reason) =>
                Task.FromResult(false);
        }

        private sealed class FakeDataStorage : IDataStorage
        {
            public PendingChildGameStateSyncEnvelope LoadEnvelope { get; set; }

            public PendingChildGameStateSyncEnvelope LastSavedEnvelope { get; private set; }

            public Task SaveAsync<T>(string key, T data)
            {
                if (data is PendingChildGameStateSyncEnvelope envelope)
                {
                    LastSavedEnvelope = CloneEnvelope(envelope);
                    LoadEnvelope = CloneEnvelope(envelope);
                }

                return Task.CompletedTask;
            }

            public Task<DataLoadResult<T>> LoadAsync<T>(string key) where T : new()
            {
                if (typeof(T) == typeof(PendingChildGameStateSyncEnvelope) && LoadEnvelope != null)
                {
                    return Task.FromResult(new DataLoadResult<T>(true, (T)(object)CloneEnvelope(LoadEnvelope)));
                }

                return Task.FromResult(new DataLoadResult<T>(false, new T()));
            }

            private static PendingChildGameStateSyncEnvelope CloneEnvelope(PendingChildGameStateSyncEnvelope envelope)
            {
                PendingChildGameStateSyncEnvelope clone = new PendingChildGameStateSyncEnvelope();
                if (envelope?.Entries == null)
                {
                    return clone;
                }

                for (int index = 0; index < envelope.Entries.Count; index++)
                {
                    PendingChildGameStateSyncEntry entry = envelope.Entries[index];
                    if (entry == null)
                    {
                        continue;
                    }

                    clone.Entries.Add(new PendingChildGameStateSyncEntry
                    {
                        RemoteProfileId = entry.RemoteProfileId,
                        BaseRevision = entry.BaseRevision,
                        Snapshot = entry.Snapshot == null
                            ? null
                            : new ChildGameStateSnapshot
                            {
                                ChildId = entry.Snapshot.ChildId,
                                Revision = entry.Snapshot.Revision,
                                CoinsBalance = entry.Snapshot.CoinsBalance,
                                BrushSessionDurationMinutes = entry.Snapshot.BrushSessionDurationMinutes,
                                PendingReward = entry.Snapshot.PendingReward,
                                Muted = entry.Snapshot.Muted,
                                PetState = entry.Snapshot.PetState,
                                RoomState = entry.Snapshot.RoomState,
                                InventoryState = entry.Snapshot.InventoryState
                            }
                    });
                }

                return clone;
            }
        }

        private sealed class FakeGameLogger : IGameLogger
        {
            public readonly List<string> Messages = new();

            public void Log(string message)
            {
                Messages.Add(message);
            }

            public void LogWarning(string message)
            {
                Messages.Add(message);
            }
        }

        private sealed class SyncStatusRecorder
        {
            public bool LastHasPendingSync { get; private set; }

            public bool LastIsOffline { get; private set; }

            public SyncStatusRecorder(EventDispatcher dispatcher)
            {
                dispatcher.Subscribe<ChildGameStateSyncStatusChangedEvent>(OnChanged);
            }

            private void OnChanged(ChildGameStateSyncStatusChangedEvent eventData)
            {
                LastHasPendingSync = eventData.HasPendingSync;
                LastIsOffline = eventData.IsOffline;
            }
        }

        private sealed class FailureRecorder
        {
            public List<string> Messages { get; } = new();

            public FailureRecorder(EventDispatcher dispatcher)
            {
                dispatcher.Subscribe<ChildGameStateSyncFailureEvent>(eventData => Messages.Add(eventData.Message));
            }
        }
    }
}
