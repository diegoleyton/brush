using System.Collections.Generic;
using System.Threading.Tasks;

using Flowbit.Utilities.Core.Events;

using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;

using NUnit.Framework;

using GameState = Game.Core.Data.Data;

namespace Game.Core.Tests
{
    public sealed class RemoteProfilesServiceTests
    {
        [Test]
        public async Task RefreshAsync_MergesRemoteProfileIntoExistingSelectedProfile()
        {
            Profile existingProfile = new Profile
            {
                RemoteProfileId = "child-1",
                Name = "Old Name",
                ProfilePictureId = 1,
                PendingRewardCount = 1,
                Coins = 99,
                PetData = new Pet { Name = "Old Pet", lastBrushTime = 123 }
            };

            ClientGameStateStore store = CreateStore(existingProfile);
            EventDispatcher dispatcher = new EventDispatcher();
            ProfileUpdatedEventRecorder updatedRecorder = new ProfileUpdatedEventRecorder(dispatcher);
            ProfileSwitchedEventRecorder switchedRecorder = new ProfileSwitchedEventRecorder(dispatcher);

            RemoteProfilesService service = new RemoteProfilesService(
                store,
                new FakeAuthService(hasSession: true),
                new FakeChildrenApiClient
                {
                    ListedProfiles = new List<Profile>
                    {
                        new Profile
                        {
                            RemoteProfileId = "child-1",
                            Name = "New Name",
                            ProfilePictureId = 7,
                            PetData = new Pet { Name = "New Pet" }
                        }
                    }
                },
                dispatcher);

            await service.RefreshAsync();

            Assert.That(store.CurrentProfile, Is.SameAs(existingProfile));
            Assert.That(store.CurrentProfile.Name, Is.EqualTo("New Name"));
            Assert.That(store.CurrentProfile.ProfilePictureId, Is.EqualTo(7));
            Assert.That(store.CurrentProfile.PetData.Name, Is.EqualTo("New Pet"));
            Assert.That(store.CurrentProfile.PendingRewardCount, Is.EqualTo(1));
            Assert.That(store.CurrentProfile.Coins, Is.EqualTo(99));
            Assert.That(updatedRecorder.Count, Is.EqualTo(1));
            Assert.That(switchedRecorder.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task RefreshAsync_SelectsFirstProfileAndEmitsSwitched_WhenCurrentSelectionNoLongerExists()
        {
            ClientGameStateStore store = CreateStore(
                new Profile { RemoteProfileId = "child-missing", Name = "Missing" });
            EventDispatcher dispatcher = new EventDispatcher();
            ProfileUpdatedEventRecorder updatedRecorder = new ProfileUpdatedEventRecorder(dispatcher);
            ProfileSwitchedEventRecorder switchedRecorder = new ProfileSwitchedEventRecorder(dispatcher);

            RemoteProfilesService service = new RemoteProfilesService(
                store,
                new FakeAuthService(hasSession: true),
                new FakeChildrenApiClient
                {
                    ListedProfiles = new List<Profile>
                    {
                        new Profile { RemoteProfileId = "child-a", Name = "Kid A" },
                        new Profile { RemoteProfileId = "child-b", Name = "Kid B" }
                    }
                },
                dispatcher);

            await service.RefreshAsync();

            Assert.That(store.CurrentProfileIndex, Is.EqualTo(0));
            Assert.That(store.CurrentProfile.RemoteProfileId, Is.EqualTo("child-a"));
            Assert.That(updatedRecorder.Count, Is.EqualTo(1));
            Assert.That(switchedRecorder.Count, Is.EqualTo(1));
        }

        [Test]
        public void SelectProfile_UpdatesSelectionAndEmitsEvents()
        {
            ClientGameStateStore store = CreateStore(
                new Profile { RemoteProfileId = "child-a", Name = "Kid A" },
                new Profile { RemoteProfileId = "child-b", Name = "Kid B" });
            EventDispatcher dispatcher = new EventDispatcher();
            ProfileSwitchedEventRecorder switchedRecorder = new ProfileSwitchedEventRecorder(dispatcher);
            LocalDataChangedEventRecorder localChangedRecorder = new LocalDataChangedEventRecorder(dispatcher);

            RemoteProfilesService service = new RemoteProfilesService(
                store,
                new FakeAuthService(hasSession: true),
                new FakeChildrenApiClient(),
                dispatcher);

            service.SelectProfile(1);

            Assert.That(store.CurrentProfileIndex, Is.EqualTo(1));
            Assert.That(store.CurrentProfile.RemoteProfileId, Is.EqualTo("child-b"));
            Assert.That(switchedRecorder.Count, Is.EqualTo(1));
            Assert.That(localChangedRecorder.Count, Is.EqualTo(1));
        }

        private static ClientGameStateStore CreateStore(params Profile[] profiles)
        {
            GameState data = new GameState();
            if (profiles != null)
            {
                for (int index = 0; index < profiles.Length; index++)
                {
                    data.Profiles.Add(profiles[index]);
                }
            }

            data.CurrentProfile = data.Profiles.Count > 0 ? 0 : -1;
            return new ClientGameStateStore(data);
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

        private sealed class FakeChildrenApiClient : IChildrenApiClient
        {
            public IReadOnlyList<Profile> ListedProfiles { get; set; } = new List<Profile>();

            public Task<IReadOnlyList<Profile>> ListAsync() =>
                Task.FromResult(ListedProfiles);

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

            public Task<MarketPurchaseStatus> PurchaseMarketItemAsync(string remoteProfileId, MarketItemDefinition itemDefinition) =>
                Task.FromResult(MarketPurchaseStatus.ITEM_NOT_FOUND);

            public Task<bool> GrantCoinsAsync(string remoteProfileId, int amount, string reason) =>
                Task.FromResult(false);
        }

        private sealed class ProfileUpdatedEventRecorder
        {
            public int Count { get; private set; }

            public ProfileUpdatedEventRecorder(EventDispatcher dispatcher)
            {
                dispatcher.Subscribe<ProfileUpdatedEvent>(_ => Count++);
            }
        }

        private sealed class ProfileSwitchedEventRecorder
        {
            public int Count { get; private set; }

            public ProfileSwitchedEventRecorder(EventDispatcher dispatcher)
            {
                dispatcher.Subscribe<ProfileSwitchedEvent>(_ => Count++);
            }
        }

        private sealed class LocalDataChangedEventRecorder
        {
            public int Count { get; private set; }

            public LocalDataChangedEventRecorder(EventDispatcher dispatcher)
            {
                dispatcher.Subscribe<LocalDataChangedEvent>(_ => Count++);
            }
        }
    }
}
