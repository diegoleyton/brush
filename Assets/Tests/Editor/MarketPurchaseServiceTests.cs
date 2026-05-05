using System.Threading.Tasks;

using Game.Core.Data;
using Game.Core.Services;

using NUnit.Framework;

using GameState = Game.Core.Data.Data;

namespace Game.Core.Tests
{
    public sealed class MarketPurchaseServiceTests
    {
        [Test]
        public async Task PurchaseAsync_ReturnsNoCurrentProfile_WhenSessionIsMissing()
        {
            ClientGameStateStore store = CreateStore(CreateProfile("child-1"));
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient();
            FakeChildGameStateSyncService syncService = new FakeChildGameStateSyncService();
            MarketPurchaseService service = new MarketPurchaseService(
                store,
                childrenApiClient,
                syncService,
                new FakeAuthService(hasSession: false));

            MarketPurchaseStatus status = await service.PurchaseAsync(CreateMarketItem());

            Assert.That(status, Is.EqualTo(MarketPurchaseStatus.NO_CURRENT_PROFILE));
            Assert.That(childrenApiClient.PurchaseMarketItemCallCount, Is.EqualTo(0));
            Assert.That(syncService.ReloadCurrentProfileCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task PurchaseAsync_ReturnsNoCurrentProfile_WhenCurrentProfileIsMissing()
        {
            ClientGameStateStore store = new ClientGameStateStore(new GameState());
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient();
            FakeChildGameStateSyncService syncService = new FakeChildGameStateSyncService();
            MarketPurchaseService service = new MarketPurchaseService(
                store,
                childrenApiClient,
                syncService,
                new FakeAuthService(hasSession: true));

            MarketPurchaseStatus status = await service.PurchaseAsync(CreateMarketItem());

            Assert.That(status, Is.EqualTo(MarketPurchaseStatus.NO_CURRENT_PROFILE));
            Assert.That(childrenApiClient.PurchaseMarketItemCallCount, Is.EqualTo(0));
            Assert.That(syncService.ReloadCurrentProfileCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task PurchaseAsync_ReturnsItemNotFound_WhenItemDefinitionIsNull()
        {
            ClientGameStateStore store = CreateStore(CreateProfile("child-2"));
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient();
            FakeChildGameStateSyncService syncService = new FakeChildGameStateSyncService();
            MarketPurchaseService service = new MarketPurchaseService(
                store,
                childrenApiClient,
                syncService,
                new FakeAuthService(hasSession: true));

            MarketPurchaseStatus status = await service.PurchaseAsync(null);

            Assert.That(status, Is.EqualTo(MarketPurchaseStatus.ITEM_NOT_FOUND));
            Assert.That(childrenApiClient.PurchaseMarketItemCallCount, Is.EqualTo(0));
            Assert.That(syncService.ReloadCurrentProfileCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task PurchaseAsync_ReloadsCurrentProfile_WhenPurchaseSucceeds()
        {
            ClientGameStateStore store = CreateStore(CreateProfile("child-3"));
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient
            {
                PurchaseResult = MarketPurchaseStatus.OK
            };
            FakeChildGameStateSyncService syncService = new FakeChildGameStateSyncService();
            MarketPurchaseService service = new MarketPurchaseService(
                store,
                childrenApiClient,
                syncService,
                new FakeAuthService(hasSession: true));

            MarketPurchaseStatus status = await service.PurchaseAsync(CreateMarketItem());

            Assert.That(status, Is.EqualTo(MarketPurchaseStatus.OK));
            Assert.That(childrenApiClient.PurchaseMarketItemCallCount, Is.EqualTo(1));
            Assert.That(childrenApiClient.LastPurchasedProfileId, Is.EqualTo("child-3"));
            Assert.That(syncService.ReloadCurrentProfileCallCount, Is.EqualTo(1));
        }

        [Test]
        public async Task PurchaseAsync_DoesNotReload_WhenPurchaseDoesNotSucceed()
        {
            ClientGameStateStore store = CreateStore(CreateProfile("child-4"));
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient
            {
                PurchaseResult = MarketPurchaseStatus.NOT_ENOUGH_CURRENCY
            };
            FakeChildGameStateSyncService syncService = new FakeChildGameStateSyncService();
            MarketPurchaseService service = new MarketPurchaseService(
                store,
                childrenApiClient,
                syncService,
                new FakeAuthService(hasSession: true));

            MarketPurchaseStatus status = await service.PurchaseAsync(CreateMarketItem());

            Assert.That(status, Is.EqualTo(MarketPurchaseStatus.NOT_ENOUGH_CURRENCY));
            Assert.That(childrenApiClient.PurchaseMarketItemCallCount, Is.EqualTo(1));
            Assert.That(syncService.ReloadCurrentProfileCallCount, Is.EqualTo(0));
        }

        private static ClientGameStateStore CreateStore(Profile currentProfile)
        {
            GameState data = new GameState();
            if (currentProfile != null)
            {
                data.Profiles.Add(currentProfile);
                data.CurrentProfile = 0;
            }

            return new ClientGameStateStore(data);
        }

        private static Profile CreateProfile(string remoteProfileId)
        {
            return new Profile
            {
                RemoteProfileId = remoteProfileId,
                Name = "Kid"
            };
        }

        private static MarketItemDefinition CreateMarketItem()
        {
            return new MarketItemDefinition
            {
                ItemType = InteractionPointType.FOOD,
                ItemId = 7,
                CurrencyType = CurrencyType.Coins,
                Price = 15,
                Quantity = 1
            };
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

            public Task<bool> EnsureSessionIsValidAsync() => Task.FromResult(HasSession);

            public Task<AuthResult> CreateAccountAsync(string email, string password, string familyName) =>
                Task.FromResult<AuthResult>(null);

            public Task<AuthResult> LoginAsync(string email, string password) =>
                Task.FromResult<AuthResult>(null);

            public Task LogoutAsync() => Task.CompletedTask;
        }

        private sealed class FakeChildGameStateSyncService : IChildGameStateSyncService
        {
            public bool HasPendingSync => false;

            public bool IsOffline => false;

            public int ReloadCurrentProfileCallCount { get; private set; }

            public Task<bool> EnsureCurrentProfileLoadedAsync() =>
                Task.FromResult(true);

            public Task<bool> ReloadCurrentProfileAsync()
            {
                ReloadCurrentProfileCallCount++;
                return Task.FromResult(true);
            }
        }

        private sealed class FakeChildrenApiClient : IChildrenApiClient
        {
            public MarketPurchaseStatus PurchaseResult { get; set; }

            public int PurchaseMarketItemCallCount { get; private set; }

            public string LastPurchasedProfileId { get; private set; }

            public MarketItemDefinition LastPurchasedItemDefinition { get; private set; }

            public Task<System.Collections.Generic.IReadOnlyList<Profile>> ListAsync() =>
                Task.FromResult<System.Collections.Generic.IReadOnlyList<Profile>>(null);

            public Task<Profile> CreateAsync(string name, string petName, int pictureId) =>
                Task.FromResult<Profile>(null);

            public Task<bool> DeleteAsync(string remoteProfileId) =>
                Task.FromResult(false);

            public Task<bool> UpdateProfileAsync(string remoteProfileId, string name, string petName, int pictureId, bool isActive) =>
                Task.FromResult(false);

            public Task<ChildGameStateSnapshot> GetGameStateAsync(string remoteProfileId) =>
                Task.FromResult<ChildGameStateSnapshot>(null);

            public Task<ChildGameStateSyncPushResult> PushGameStateAsync(string remoteProfileId, string baseRevision, ChildGameStateSnapshot snapshot) =>
                Task.FromResult<ChildGameStateSyncPushResult>(null);

            public Task<ChildGameStateSnapshot> CompleteBrushSessionAsync(string remoteProfileId) =>
                Task.FromResult<ChildGameStateSnapshot>(null);

            public Task<Reward[]> ClaimRewardsAsync(string remoteProfileId) =>
                Task.FromResult<Reward[]>(null);

            public Task<MarketPurchaseStatus> PurchaseMarketItemAsync(string remoteProfileId, MarketItemDefinition itemDefinition)
            {
                PurchaseMarketItemCallCount++;
                LastPurchasedProfileId = remoteProfileId;
                LastPurchasedItemDefinition = itemDefinition;
                return Task.FromResult(PurchaseResult);
            }

            public Task<bool> GrantCoinsAsync(string remoteProfileId, int amount, string reason) =>
                Task.FromResult(false);
        }
    }
}
