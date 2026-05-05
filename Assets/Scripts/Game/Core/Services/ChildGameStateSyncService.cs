using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
using Flowbit.Utilities.Storage;
using Game.Core.Data;
using Game.Core.Events;
using Zenject;

namespace Game.Core.Services
{
    /// <summary>
    /// Keeps the currently selected child profile hydrated from the backend, while supporting optimistic local updates
    /// with persisted pending sync when transport-level failures happen.
    /// </summary>
    public sealed class ChildGameStateSyncService : IChildGameStateSyncService, IInitializable, IDisposable
    {
        private const string PendingSyncStorageKey = "child_game_state_pending_sync";
        private const int BackgroundSyncIntervalMilliseconds = 5000;

        private readonly EventDispatcher dispatcher_;
        private readonly ClientGameStateStore gameStateStore_;
        private readonly IChildrenApiClient childrenApiClient_;
        private readonly IAuthService authService_;
        private readonly IDataStorage dataStorage_;
        private readonly IGameLogger logger_;
        private readonly HashSet<string> hydratedProfileIds_ = new(StringComparer.Ordinal);

        private PendingChildGameStateSyncEnvelope pendingSyncEnvelope_ = new PendingChildGameStateSyncEnvelope();
        private Task ensurePendingSyncLoadedTask_;
        private bool isDisposed_;
        private bool isApplyingRemoteState_;
        private bool isPushInFlight_;
        private bool pushRequestedWhileInFlight_;
        private bool isOffline_;

        public bool HasPendingSync => HasPendingSyncEntries();

        public bool IsOffline => isOffline_;

        public ChildGameStateSyncService(
            EventDispatcher dispatcher,
            ClientGameStateStore gameStateStore,
            IChildrenApiClient childrenApiClient,
            IAuthService authService,
            IDataStorage dataStorage,
            IGameLogger logger)
        {
            dispatcher_ = dispatcher;
            gameStateStore_ = gameStateStore;
            childrenApiClient_ = childrenApiClient;
            authService_ = authService;
            dataStorage_ = dataStorage;
            logger_ = logger;
        }

        public void Initialize()
        {
            dispatcher_?.Subscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_?.Subscribe<ChildGameStateLocallyChangedEvent>(OnLocalDataChanged);
            _ = EnsurePendingSyncLoadedAsync();
            _ = RunBackgroundSyncLoopAsync();
        }

        public void Dispose()
        {
            if (isDisposed_)
            {
                return;
            }

            isDisposed_ = true;
            dispatcher_?.Unsubscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_?.Unsubscribe<ChildGameStateLocallyChangedEvent>(OnLocalDataChanged);
        }

        public async Task<bool> EnsureCurrentProfileLoadedAsync()
        {
            return await LoadCurrentProfileAsync(forceReload: false);
        }

        public async Task<bool> ReloadCurrentProfileAsync()
        {
            return await LoadCurrentProfileAsync(forceReload: true);
        }

        private async Task<bool> LoadCurrentProfileAsync(bool forceReload)
        {
            await EnsurePendingSyncLoadedAsync();

            if (!CanSyncCurrentProfile(out Profile currentProfile))
            {
                return true;
            }

            string remoteProfileId = currentProfile.RemoteProfileId;
            PendingChildGameStateSyncEntry pendingEntry = FindPendingEntry(remoteProfileId);

            if (!forceReload && pendingEntry == null && hydratedProfileIds_.Contains(remoteProfileId))
            {
                return true;
            }

            ChildGameStateSnapshot snapshot = await childrenApiClient_.GetGameStateAsync(remoteProfileId);
            if (snapshot == null)
            {
                isOffline_ = true;
                PublishSyncStatus();
                return false;
            }

            isOffline_ = false;
            hydratedProfileIds_.Add(remoteProfileId);

            if (pendingEntry != null)
            {
                if (string.Equals(pendingEntry.BaseRevision, snapshot.Revision, StringComparison.Ordinal))
                {
                    await FlushPendingSyncAsync(remoteProfileId);
                    return true;
                }

                ApplySnapshot(currentProfile, snapshot, preserveExistingDefaults: false);
                await RemovePendingEntryAsync(remoteProfileId);
                PublishFailure("Local changes were discarded because newer data exists on the server.");
                return true;
            }

            bool remoteStateWasUninitialized = IsUninitializedSnapshot(snapshot);
            ApplySnapshot(currentProfile, snapshot, remoteStateWasUninitialized);

            if (remoteStateWasUninitialized)
            {
                UpsertPendingEntry(currentProfile);
                await SavePendingSyncEnvelopeAsync();
                PublishSyncStatus();
                await FlushPendingSyncAsync(remoteProfileId);
            }
            else
            {
                PublishSyncStatus();
            }

            return true;
        }

        private async void OnProfileSwitched(ProfileSwitchedEvent _)
        {
            try
            {
                await EnsureCurrentProfileLoadedAsync();
            }
            catch (AuthSessionInvalidatedException)
            {
                // The global presenter owns the player-facing flow for invalid sessions.
            }
        }

        private async void OnLocalDataChanged(ChildGameStateLocallyChangedEvent _)
        {
            if (isApplyingRemoteState_ || isDisposed_)
            {
                return;
            }

            await EnsurePendingSyncLoadedAsync();

            if (!CanSyncCurrentProfile(out Profile currentProfile))
            {
                return;
            }

            if (!hydratedProfileIds_.Contains(currentProfile.RemoteProfileId))
            {
                return;
            }

            UpsertPendingEntry(currentProfile);
            await SavePendingSyncEnvelopeAsync();
            PublishSyncStatus();

            await FlushPendingSyncAsync(currentProfile.RemoteProfileId);
        }

        private async Task FlushPendingSyncAsync(string preferredRemoteProfileId = null)
        {
            if (isPushInFlight_)
            {
                pushRequestedWhileInFlight_ = true;
                return;
            }

            isPushInFlight_ = true;
            try
            {
                do
                {
                    pushRequestedWhileInFlight_ = false;

                    List<PendingChildGameStateSyncEntry> flushOrder = BuildFlushOrder(preferredRemoteProfileId);
                    for (int index = 0; index < flushOrder.Count; index++)
                    {
                        PendingChildGameStateSyncEntry entry = flushOrder[index];
                        if (entry == null || string.IsNullOrWhiteSpace(entry.RemoteProfileId))
                        {
                            continue;
                        }

                        ChildGameStateSyncPushResult result = await childrenApiClient_.PushGameStateAsync(
                            entry.RemoteProfileId,
                            entry.BaseRevision,
                            entry.Snapshot);

                        if (result == null)
                        {
                            isOffline_ = true;
                            PublishSyncStatus();
                            return;
                        }

                        switch (result.Status)
                        {
                            case ChildGameStateSyncPushStatus.Success:
                                await HandleSuccessfulPushAsync(entry.RemoteProfileId, result.Snapshot ?? entry.Snapshot);
                                break;

                            case ChildGameStateSyncPushStatus.TransportFailure:
                                isOffline_ = true;
                                PublishSyncStatus();
                                return;

                            case ChildGameStateSyncPushStatus.Conflict:
                            case ChildGameStateSyncPushStatus.ServerRejected:
                                isOffline_ = false;
                                await HandleRejectedPushAsync(entry.RemoteProfileId, result.ErrorMessage);
                                return;

                            case ChildGameStateSyncPushStatus.SessionInvalidated:
                                isOffline_ = false;
                                await RemovePendingEntryAsync(entry.RemoteProfileId);
                                PublishSyncStatus();
                                return;

                            default:
                                isOffline_ = true;
                                PublishSyncStatus();
                                return;
                        }
                    }
                }
                while (pushRequestedWhileInFlight_ && !isDisposed_);
            }
            catch (Exception exception)
            {
                isOffline_ = true;
                logger_?.Log($"[GameStateSync] Failed to synchronize child game state: {exception.Message}");
                PublishSyncStatus();
            }
            finally
            {
                isPushInFlight_ = false;
            }
        }

        private async Task HandleSuccessfulPushAsync(string remoteProfileId, ChildGameStateSnapshot snapshot)
        {
            isOffline_ = false;

            Profile profile = FindProfile(remoteProfileId);
            if (profile != null && snapshot != null)
            {
                ApplySnapshot(profile, snapshot, preserveExistingDefaults: false);
            }

            await RemovePendingEntryAsync(remoteProfileId);
            PublishSyncStatus();
        }

        private async Task HandleRejectedPushAsync(string remoteProfileId, string errorMessage)
        {
            await RemovePendingEntryAsync(remoteProfileId);

            ChildGameStateSnapshot latestSnapshot = await childrenApiClient_.GetGameStateAsync(remoteProfileId);
            if (latestSnapshot != null)
            {
                Profile profile = FindProfile(remoteProfileId);
                if (profile != null)
                {
                    ApplySnapshot(profile, latestSnapshot, preserveExistingDefaults: false);
                    hydratedProfileIds_.Add(remoteProfileId);
                }
            }

            PublishSyncStatus();
            PublishFailure(string.IsNullOrWhiteSpace(errorMessage)
                ? "Changes could not be saved."
                : errorMessage);
        }

        private void ApplySnapshot(Profile profile, ChildGameStateSnapshot snapshot, bool preserveExistingDefaults)
        {
            if (profile == null || snapshot == null)
            {
                return;
            }

            isApplyingRemoteState_ = true;
            try
            {
                profile.RemoteGameStateRevision = snapshot.Revision ?? string.Empty;
                profile.Coins = Math.Max(0, snapshot.CoinsBalance);

                if (!preserveExistingDefaults && snapshot.BrushSessionDurationMinutes > 0)
                {
                    profile.BrushSessionDurationMinutes = snapshot.BrushSessionDurationMinutes;
                    profile.PendingReward = snapshot.PendingReward;
                    profile.Muted = snapshot.Muted;
                }

                if (!preserveExistingDefaults && snapshot.PetState != null)
                {
                    profile.PetData = snapshot.PetState;
                }

                if (!preserveExistingDefaults && snapshot.RoomState != null)
                {
                    profile.RoomData = snapshot.RoomState;
                }

                if (!preserveExistingDefaults && snapshot.InventoryState != null)
                {
                    profile.InventoryData = snapshot.InventoryState;
                }

                dispatcher_?.Send(new ProfileUpdatedEvent());
                dispatcher_?.Send(new PendingRewardEvent());
                dispatcher_?.Send(new InventoryUpdatedEvent());
                dispatcher_?.Send(new CurrencyUpdatedEvent());
            }
            finally
            {
                isApplyingRemoteState_ = false;
            }
        }

        private static ChildGameStateSnapshot CreateSnapshot(Profile profile)
        {
            return new ChildGameStateSnapshot
            {
                ChildId = profile.RemoteProfileId ?? string.Empty,
                Revision = profile.RemoteGameStateRevision ?? string.Empty,
                CoinsBalance = profile.Coins,
                BrushSessionDurationMinutes = (int)Math.Max(1f, MathF.Round(profile.BrushSessionDurationMinutes)),
                PendingReward = profile.PendingReward,
                Muted = profile.Muted,
                PetState = profile.PetData ?? new Pet(),
                RoomState = profile.RoomData ?? new Room(),
                InventoryState = profile.InventoryData ?? new Inventory()
            };
        }

        private async Task EnsurePendingSyncLoadedAsync()
        {
            if (ensurePendingSyncLoadedTask_ == null)
            {
                ensurePendingSyncLoadedTask_ = LoadPendingSyncEnvelopeAsync();
            }

            await ensurePendingSyncLoadedTask_;
        }

        private async Task LoadPendingSyncEnvelopeAsync()
        {
            DataLoadResult<PendingChildGameStateSyncEnvelope> loadResult =
                await dataStorage_.LoadAsync<PendingChildGameStateSyncEnvelope>(PendingSyncStorageKey);

            pendingSyncEnvelope_ = loadResult.Found && loadResult.Data != null
                ? loadResult.Data
                : new PendingChildGameStateSyncEnvelope();

            pendingSyncEnvelope_.Entries ??= new List<PendingChildGameStateSyncEntry>();
            PublishSyncStatus();
        }

        private async Task SavePendingSyncEnvelopeAsync()
        {
            pendingSyncEnvelope_ ??= new PendingChildGameStateSyncEnvelope();
            pendingSyncEnvelope_.Entries ??= new List<PendingChildGameStateSyncEntry>();
            await dataStorage_.SaveAsync(PendingSyncStorageKey, pendingSyncEnvelope_);
        }

        private void UpsertPendingEntry(Profile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.RemoteProfileId))
            {
                return;
            }

            pendingSyncEnvelope_ ??= new PendingChildGameStateSyncEnvelope();
            pendingSyncEnvelope_.Entries ??= new List<PendingChildGameStateSyncEntry>();

            PendingChildGameStateSyncEntry existingEntry = FindPendingEntry(profile.RemoteProfileId);
            ChildGameStateSnapshot snapshot = CreateSnapshot(profile);

            if (existingEntry == null)
            {
                pendingSyncEnvelope_.Entries.Add(new PendingChildGameStateSyncEntry
                {
                    RemoteProfileId = profile.RemoteProfileId,
                    BaseRevision = profile.RemoteGameStateRevision ?? string.Empty,
                    Snapshot = snapshot
                });
                return;
            }

            existingEntry.Snapshot = snapshot;
        }

        private PendingChildGameStateSyncEntry FindPendingEntry(string remoteProfileId)
        {
            if (pendingSyncEnvelope_?.Entries == null || string.IsNullOrWhiteSpace(remoteProfileId))
            {
                return null;
            }

            for (int index = 0; index < pendingSyncEnvelope_.Entries.Count; index++)
            {
                PendingChildGameStateSyncEntry entry = pendingSyncEnvelope_.Entries[index];
                if (entry != null &&
                    string.Equals(entry.RemoteProfileId, remoteProfileId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private async Task RemovePendingEntryAsync(string remoteProfileId)
        {
            if (pendingSyncEnvelope_?.Entries == null || string.IsNullOrWhiteSpace(remoteProfileId))
            {
                return;
            }

            pendingSyncEnvelope_.Entries.RemoveAll(entry =>
                entry != null && string.Equals(entry.RemoteProfileId, remoteProfileId, StringComparison.Ordinal));

            await SavePendingSyncEnvelopeAsync();
        }

        private List<PendingChildGameStateSyncEntry> BuildFlushOrder(string preferredRemoteProfileId)
        {
            List<PendingChildGameStateSyncEntry> order = new List<PendingChildGameStateSyncEntry>();
            if (pendingSyncEnvelope_?.Entries == null || pendingSyncEnvelope_.Entries.Count == 0)
            {
                return order;
            }

            if (!string.IsNullOrWhiteSpace(preferredRemoteProfileId))
            {
                PendingChildGameStateSyncEntry preferredEntry = FindPendingEntry(preferredRemoteProfileId);
                if (preferredEntry != null)
                {
                    order.Add(preferredEntry);
                }
            }

            for (int index = 0; index < pendingSyncEnvelope_.Entries.Count; index++)
            {
                PendingChildGameStateSyncEntry entry = pendingSyncEnvelope_.Entries[index];
                if (entry == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(preferredRemoteProfileId) &&
                    string.Equals(entry.RemoteProfileId, preferredRemoteProfileId, StringComparison.Ordinal))
                {
                    continue;
                }

                order.Add(entry);
            }

            return order;
        }

        private Profile FindProfile(string remoteProfileId)
        {
            if (gameStateStore_?.AllProfiles == null || string.IsNullOrWhiteSpace(remoteProfileId))
            {
                return null;
            }

            for (int index = 0; index < gameStateStore_.AllProfiles.Count; index++)
            {
                Profile profile = gameStateStore_.AllProfiles[index];
                if (profile != null &&
                    string.Equals(profile.RemoteProfileId, remoteProfileId, StringComparison.Ordinal))
                {
                    return profile;
                }
            }

            return null;
        }

        private async Task RunBackgroundSyncLoopAsync()
        {
            while (!isDisposed_)
            {
                try
                {
                    await Task.Delay(BackgroundSyncIntervalMilliseconds);
                }
                catch
                {
                    return;
                }

                if (isDisposed_)
                {
                    return;
                }

                await EnsurePendingSyncLoadedAsync();

                if (authService_ == null || !authService_.HasSession || !HasPendingSyncEntries())
                {
                    continue;
                }

                await FlushPendingSyncAsync();
            }
        }

        private bool HasPendingSyncEntries() =>
            pendingSyncEnvelope_?.Entries != null && pendingSyncEnvelope_.Entries.Count > 0;

        private void PublishSyncStatus()
        {
            dispatcher_?.Send(new ChildGameStateSyncStatusChangedEvent(
                hasPendingSync: HasPendingSyncEntries(),
                isOffline: isOffline_));
        }

        private void PublishFailure(string message)
        {
            dispatcher_?.Send(new ChildGameStateSyncFailureEvent(message));
        }

        private static bool IsUninitializedSnapshot(ChildGameStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return true;
            }

            return IsEmptyPetState(snapshot.PetState) &&
                   IsEmptyRoomState(snapshot.RoomState) &&
                   IsEmptyInventoryState(snapshot.InventoryState);
        }

        private static bool IsEmptyPetState(Pet pet)
        {
            if (pet == null)
            {
                return true;
            }

            return string.IsNullOrWhiteSpace(pet.Name) &&
                   pet.lastEatTime == 0 &&
                   pet.eatCount == 0 &&
                   pet.lastBrushTime == 0 &&
                   pet.EyesItemId == 0 &&
                   pet.SkinItemId == 0 &&
                   pet.HatItemId == 0 &&
                   pet.DressItemId == 0;
        }

        private static bool IsEmptyRoomState(Room room)
        {
            if (room == null)
            {
                return true;
            }

            return (room.PlaceableObjects == null || room.PlaceableObjects.Count == 0) &&
                   (room.PaintedSurfaces == null || room.PaintedSurfaces.Count == 0);
        }

        private static bool IsEmptyInventoryState(Inventory inventory)
        {
            if (inventory == null)
            {
                return true;
            }

            return IsEmptyInventoryBucket(inventory.PlaceableObjects) &&
                   IsEmptyInventoryBucket(inventory.Paint) &&
                   IsEmptyInventoryBucket(inventory.Food) &&
                   IsEmptyInventoryBucket(inventory.Skin) &&
                   IsEmptyInventoryBucket(inventory.Hat) &&
                   IsEmptyInventoryBucket(inventory.Dress) &&
                   IsEmptyInventoryBucket(inventory.Eyes);
        }

        private static bool IsEmptyInventoryBucket(Dictionary<int, int> items) =>
            items == null || items.Count == 0;

        private bool CanSyncCurrentProfile(out Profile currentProfile)
        {
            currentProfile = gameStateStore_?.CurrentProfile;

            if (authService_ == null || !authService_.HasSession || currentProfile == null)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(currentProfile.RemoteProfileId);
        }
    }
}
