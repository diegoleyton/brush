using System.Threading.Tasks;

using Game.Core.Data;
using Game.Core.Services;

using NUnit.Framework;

using GameState = Game.Core.Data.Data;

namespace Game.Core.Tests
{
    public sealed class BrushCompletionServiceTests
    {
        [Test]
        public async Task CompleteCurrentAsync_ReturnsFalse_WhenSessionIsMissing()
        {
            ClientGameStateStore store = CreateStore(CreateProfile("child-1"));
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient();
            BrushCompletionService service = new BrushCompletionService(
                store,
                childrenApiClient,
                new FakeAuthService(hasSession: false));

            bool completed = await service.CompleteCurrentAsync();

            Assert.That(completed, Is.False);
            Assert.That(childrenApiClient.CompleteBrushSessionCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task CompleteCurrentAsync_ReturnsFalse_WhenCurrentProfileIsMissing()
        {
            ClientGameStateStore store = new ClientGameStateStore(new GameState());
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient();
            BrushCompletionService service = new BrushCompletionService(
                store,
                childrenApiClient,
                new FakeAuthService(hasSession: true));

            bool completed = await service.CompleteCurrentAsync();

            Assert.That(completed, Is.False);
            Assert.That(childrenApiClient.CompleteBrushSessionCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task CompleteCurrentAsync_CallsApiWithCurrentRemoteProfileId()
        {
            ClientGameStateStore store = CreateStore(CreateProfile("child-42"));
            FakeChildrenApiClient childrenApiClient = new FakeChildrenApiClient();
            BrushCompletionService service = new BrushCompletionService(
                store,
                childrenApiClient,
                new FakeAuthService(hasSession: true));

            bool completed = await service.CompleteCurrentAsync();

            Assert.That(completed, Is.True);
            Assert.That(childrenApiClient.CompleteBrushSessionCallCount, Is.EqualTo(1));
            Assert.That(childrenApiClient.LastCompletedBrushProfileId, Is.EqualTo("child-42"));
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
            public int CompleteBrushSessionCallCount { get; private set; }

            public string LastCompletedBrushProfileId { get; private set; }

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

            public Task<ChildGameStateSnapshot> CompleteBrushSessionAsync(string remoteProfileId)
            {
                CompleteBrushSessionCallCount++;
                LastCompletedBrushProfileId = remoteProfileId;
                return Task.FromResult<ChildGameStateSnapshot>(null);
            }

            public Task<Reward[]> ClaimRewardsAsync(string remoteProfileId) =>
                Task.FromResult<Reward[]>(null);

            public Task<MarketPurchaseStatus> PurchaseMarketItemAsync(string remoteProfileId, MarketItemDefinition itemDefinition) =>
                Task.FromResult(MarketPurchaseStatus.ITEM_NOT_FOUND);

            public Task<bool> GrantCoinsAsync(string remoteProfileId, int amount, string reason) =>
                Task.FromResult(false);
        }
    }
}
