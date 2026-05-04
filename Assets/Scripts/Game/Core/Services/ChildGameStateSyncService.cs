using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
using Game.Core.Data;
using Game.Core.Events;
using Zenject;

namespace Game.Core.Services
{
    /// <summary>
    /// Keeps the currently selected child profile hydrated from the backend and pushes local state changes back.
    /// Coins are intentionally left local for now until the economy flow is fully migrated.
    /// </summary>
    public sealed class ChildGameStateSyncService : IChildGameStateSyncService, IInitializable, IDisposable
    {
        private readonly EventDispatcher dispatcher_;
        private readonly ClientGameStateStore gameStateStore_;
        private readonly IChildrenApiClient childrenApiClient_;
        private readonly IAuthService authService_;
        private readonly IGameLogger logger_;
        private readonly HashSet<string> hydratedProfileIds_ = new(StringComparer.Ordinal);

        private bool isDisposed_;
        private bool isApplyingRemoteState_;
        private bool isPushInFlight_;
        private bool pushRequestedWhileInFlight_;

        public ChildGameStateSyncService(
            EventDispatcher dispatcher,
            ClientGameStateStore gameStateStore,
            IChildrenApiClient childrenApiClient,
            IAuthService authService,
            IGameLogger logger)
        {
            dispatcher_ = dispatcher;
            gameStateStore_ = gameStateStore;
            childrenApiClient_ = childrenApiClient;
            authService_ = authService;
            logger_ = logger;
        }

        public void Initialize()
        {
            dispatcher_?.Subscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_?.Subscribe<LocalDataChangedEvent>(OnLocalDataChanged);
        }

        public void Dispose()
        {
            if (isDisposed_)
            {
                return;
            }

            isDisposed_ = true;
            dispatcher_?.Unsubscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_?.Unsubscribe<LocalDataChangedEvent>(OnLocalDataChanged);
        }

        public async Task EnsureCurrentProfileLoadedAsync()
        {
            if (!CanSyncCurrentProfile(out Profile currentProfile))
            {
                return;
            }

            string remoteProfileId = currentProfile.RemoteProfileId;
            if (hydratedProfileIds_.Contains(remoteProfileId))
            {
                logger_?.Log($"[GameStateSync] Skipping load for {remoteProfileId} because it is already hydrated.");
                return;
            }

            logger_?.Log($"[GameStateSync] Loading child game state for profile {remoteProfileId}.");
            ChildGameStateSnapshot snapshot = await childrenApiClient_.GetGameStateAsync(remoteProfileId);
            if (snapshot == null)
            {
                logger_?.Log($"[GameStateSync] No child game state snapshot returned for profile {remoteProfileId}.");
                return;
            }

            bool remoteStateWasUninitialized = IsUninitializedSnapshot(snapshot);
            logger_?.Log(
                $"[GameStateSync] Snapshot for {remoteProfileId} uninitialized={remoteStateWasUninitialized}.");
            ApplySnapshot(currentProfile, snapshot, remoteStateWasUninitialized);
            hydratedProfileIds_.Add(remoteProfileId);

            if (remoteStateWasUninitialized)
            {
                logger_?.Log($"[GameStateSync] Seeding remote child game state for profile {remoteProfileId} from local defaults.");
                await PushCurrentProfileStateAsync();
            }
        }

        private async void OnProfileSwitched(ProfileSwitchedEvent _)
        {
            await EnsureCurrentProfileLoadedAsync();
        }

        private async void OnLocalDataChanged(LocalDataChangedEvent _)
        {
            if (isApplyingRemoteState_ || isDisposed_)
            {
                return;
            }

            if (!CanSyncCurrentProfile(out Profile currentProfile))
            {
                return;
            }

            if (!hydratedProfileIds_.Contains(currentProfile.RemoteProfileId))
            {
                return;
            }

            if (isPushInFlight_)
            {
                pushRequestedWhileInFlight_ = true;
                return;
            }

            await PushCurrentProfileStateAsync();
        }

        private async Task PushCurrentProfileStateAsync()
        {
            if (!CanSyncCurrentProfile(out Profile currentProfile))
            {
                return;
            }

            isPushInFlight_ = true;
            try
            {
                do
                {
                    pushRequestedWhileInFlight_ = false;

                    await childrenApiClient_.UpdateProfileAsync(
                        currentProfile.RemoteProfileId,
                        currentProfile.Name,
                        currentProfile.PetData?.Name ?? string.Empty,
                        currentProfile.ProfilePictureId,
                        isActive: true);

                    ChildGameStateSnapshot snapshot = CreateSnapshot(currentProfile);
                    bool updated = await childrenApiClient_.UpdateGameStateAsync(currentProfile.RemoteProfileId, snapshot);
                    if (!updated)
                    {
                        logger_?.Log($"[GameStateSync] Failed to push child game state for profile {currentProfile.RemoteProfileId}.");
                        return;
                    }
                }
                while (pushRequestedWhileInFlight_);
            }
            catch (Exception exception)
            {
                logger_?.Log($"[GameStateSync] Failed to push child game state: {exception.Message}");
            }
            finally
            {
                isPushInFlight_ = false;
            }
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

                logger_?.Log(
                    $"[GameStateSync] Applied snapshot to profile {profile.RemoteProfileId}. " +
                    $"preserveExistingDefaults={preserveExistingDefaults}, " +
                    $"petName={(profile.PetData?.Name ?? "<null>")}, " +
                    $"inventory={DescribeInventory(profile.InventoryData)}.");

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
                CoinsBalance = profile.Coins,
                BrushSessionDurationMinutes = (int)Math.Max(1f, MathF.Round(profile.BrushSessionDurationMinutes)),
                PendingReward = profile.PendingReward,
                Muted = profile.Muted,
                PetState = profile.PetData ?? new Pet(),
                RoomState = profile.RoomData ?? new Room(),
                InventoryState = profile.InventoryData ?? new Inventory()
            };
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

        private static bool IsEmptyInventoryBucket(System.Collections.Generic.Dictionary<int, int> items) =>
            items == null || items.Count == 0;

        private static string DescribeInventory(Inventory inventory)
        {
            if (inventory == null)
            {
                return "<null>";
            }

            return
                $"placeable:{inventory.PlaceableObjects?.Count ?? 0}, " +
                $"paint:{inventory.Paint?.Count ?? 0}, " +
                $"food:{inventory.Food?.Count ?? 0}, " +
                $"skin:{inventory.Skin?.Count ?? 0}, " +
                $"hat:{inventory.Hat?.Count ?? 0}, " +
                $"dress:{inventory.Dress?.Count ?? 0}, " +
                $"eyes:{inventory.Eyes?.Count ?? 0}";
        }

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
