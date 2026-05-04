using System;
using System.Collections.Generic;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Rules;
using GameState = Game.Core.Data.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Central repository responsible for reading and mutating persisted game state.
    /// </summary>
    public class DataRepository
    {
        private readonly EventDispatcher dispatcher_;
        private readonly IGameLogger logger_;
        private readonly ClientGameStateStore gameStateStore_;
        private readonly IProfileService profileService_;

        public GameState Data => gameStateStore_.Data;

        public Profile CurrentProfile => gameStateStore_.CurrentProfile;

        public List<Profile> AllProfiles => gameStateStore_.AllProfiles;

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public DataRepository(
            ClientGameStateStore gameStateStore,
            IProfileService profileService,
            EventDispatcher dispatcher,
            IGameLogger logger)
        {
            gameStateStore_ = gameStateStore ?? throw new ArgumentNullException(nameof(gameStateStore));
            profileService_ = profileService ?? throw new ArgumentNullException(nameof(profileService));
            dispatcher_ = dispatcher;
            logger_ = logger;
        }

        public bool CanUseName(string name) => profileService_.CanUseName(name);

        public bool CanUsePetName(string name) => profileService_.CanUsePetName(name);

        public Profile CreateProfile(string name, string petName, int pictureId) =>
            profileService_.CreateProfile(name, petName, pictureId);

        public void ModifyCurrentProfile(string name, string petName, int pictureId) =>
            profileService_.ModifyCurrentProfile(name, petName, pictureId);

        public void SetPendingReward(bool pendingReward)
        {
            CurrentProfile.PendingReward = pendingReward;
            dispatcher_.Send(new PendingRewardEvent());
            NotifyDataChanged();
        }

        public int GetCurrencyBalance(CurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CurrencyType.Coins:
                    return CurrentProfile?.Coins ?? 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currencyType), currencyType, null);
            }
        }

        public void AddCurrency(CurrencyType currencyType, int amount)
        {
            if (CurrentProfile == null || amount <= 0)
            {
                return;
            }

            switch (currencyType)
            {
                case CurrencyType.Coins:
                    CurrentProfile.Coins += amount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currencyType), currencyType, null);
            }

            dispatcher_.Send(new CurrencyUpdatedEvent());
            NotifyDataChanged();
        }

        public void AddCurrencyReward(CurrencyReward reward)
        {
            if (reward == null)
            {
                return;
            }

            AddCurrency(reward.CurrencyType, reward.Amount);
        }

        public bool CanAfford(CurrencyType currencyType, int amount) =>
            amount >= 0 && GetCurrencyBalance(currencyType) >= amount;

        public MarketPurchaseStatus PurchaseMarketItem(InteractionPointType itemType, int itemId)
        {
            if (CurrentProfile == null)
            {
                return MarketPurchaseStatus.NO_CURRENT_PROFILE;
            }

            if (!MarketCatalog.TryGet(itemType, itemId, out MarketItemDefinition itemDefinition))
            {
                dispatcher_.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.ITEM_NOT_FOUND, null));
                return MarketPurchaseStatus.ITEM_NOT_FOUND;
            }

            if (IsMarketItemAlreadyOwned(itemType, itemId))
            {
                dispatcher_.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.ALREADY_OWNED, itemDefinition));
                return MarketPurchaseStatus.ALREADY_OWNED;
            }

            Dictionary<int, int> items = CurrentProfile.InventoryData.GetInventoryItems(itemType);
            if (items.TryGetValue(itemId, out int currentQuantity) && currentQuantity == -1)
            {
                dispatcher_.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.ALREADY_OWNED, itemDefinition));
                return MarketPurchaseStatus.ALREADY_OWNED;
            }

            if (!CanAfford(itemDefinition.CurrencyType, itemDefinition.Price))
            {
                dispatcher_.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.NOT_ENOUGH_CURRENCY, itemDefinition));
                return MarketPurchaseStatus.NOT_ENOUGH_CURRENCY;
            }

            SpendCurrency(itemDefinition.CurrencyType, itemDefinition.Price);
            if (itemType == InteractionPointType.DRESS)
            {
                UnlockOwnedDress(itemId);
            }
            else
            {
                AddInventoryItem(itemType, itemId, itemDefinition.Quantity);
            }

            NotifyInventoryChanged();
            dispatcher_.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.OK, itemDefinition));
            NotifyDataChanged();
            return MarketPurchaseStatus.OK;
        }

        public Reward[] GiveRewards()
        {
            SetPendingReward(false);
            Reward[] rewards = new Reward[2];

            rewards[0] = GameRules.GetGuaranteedFoodReward();
            rewards[1] = GameRules.GetRandomReward();

            foreach (Reward reward in rewards)
            {
                ApplyReward(reward);
            }

            NotifyDataChanged();
            NotifyInventoryChanged();
            return rewards;
        }

        public void SwitchProfile(int index) => profileService_.SwitchProfile(index);

        public bool DeleteProfile(int index) => profileService_.DeleteProfile(index);

        public void SetPetName(string name) => profileService_.SetPetName(name);

        public float GetBrushSessionDurationMinutes()
        {
            if (CurrentProfile == null ||
                CurrentProfile.BrushSessionDurationMinutes <= 0f)
            {
                return DefaultProfileState.DefaultBrushSessionDurationMinutes;
            }

            return CurrentProfile.BrushSessionDurationMinutes;
        }

        public void SetBrushSessionDurationMinutes(float minutes)
        {
            if (Data?.Profiles == null || Data.Profiles.Count == 0)
            {
                return;
            }

            float sanitizedMinutes = Math.Max(0.1f, minutes);
            bool changed = false;

            for (int index = 0; index < Data.Profiles.Count; index++)
            {
                Profile profile = Data.Profiles[index];
                if (profile == null)
                {
                    continue;
                }

                if (Math.Abs(profile.BrushSessionDurationMinutes - sanitizedMinutes) < 0.001f)
                {
                    continue;
                }

                profile.BrushSessionDurationMinutes = sanitizedMinutes;
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            dispatcher_.Send(new ProfileUpdatedEvent());
            NotifyDataChanged();
        }

        public PetEatStatus CanPetEat
        {
            get
            {
                long now = Now;
                if ((now - CurrentProfile.PetData.lastBrushTime) < GameRules.EatBlockedAfterBrushSeconds)
                {
                    return PetEatStatus.NO_AFTER_BRUSHING;
                }

                if ((now - CurrentProfile.PetData.lastEatTime) < GameRules.EatLimitWindowSeconds &&
                    CurrentProfile.PetData.eatCount >= 3)
                {
                    return PetEatStatus.NO_MORE;
                }

                return PetEatStatus.OK;
            }
        }

        public bool IsTimeToBrush => Now - CurrentProfile.PetData.lastBrushTime > GameRules.BrushCooldownSeconds;

        public bool RoomHasObject(int locationId, int itemId)
        {
            PlacedRoomObjectLocation roomObject = FindRoomObject(locationId);
            return roomObject?.Item != null && roomObject.Item.ItemId == itemId;
        }

        public bool RoomSurfaceHasPaint(int surfaceId, int paintItemId)
        {
            RoomPaintSurfaceState surface = FindRoomSurface(surfaceId);
            return surface != null && surface.PaintId == paintItemId;
        }

        public int GetRoomSurfacePaintId(int surfaceId)
        {
            RoomPaintSurfaceState surface = FindRoomSurface(surfaceId);
            return surface?.PaintId ?? DefaultProfileState.NoPaintItemId;
        }

        public int GetAppliedPetItemId(InteractionPointType interactionPointType)
        {
            if (CurrentProfile?.PetData == null)
            {
                return 0;
            }

            switch (interactionPointType)
            {
                case InteractionPointType.EYES:
                    return CurrentProfile.PetData.EyesItemId;
                case InteractionPointType.SKIN:
                    return CurrentProfile.PetData.SkinItemId;
                case InteractionPointType.HAT:
                    return CurrentProfile.PetData.HatItemId;
                case InteractionPointType.DRESS:
                    return CurrentProfile.PetData.DressItemId;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interactionPointType), interactionPointType, null);
            }
        }

        public bool IsMarketItemAlreadyOwned(InteractionPointType itemType, int itemId)
        {
            if (CurrentProfile?.InventoryData == null)
            {
                return false;
            }

            if (itemType == InteractionPointType.DRESS &&
                CurrentProfile.PetData != null &&
                CurrentProfile.PetData.DressItemId == itemId)
            {
                return true;
            }

            Dictionary<int, int> items = CurrentProfile.InventoryData.GetInventoryItems(itemType);
            return items.TryGetValue(itemId, out int quantity) && quantity == -1;
        }

        public void PetEat()
        {
            if ((Now - CurrentProfile.PetData.lastEatTime) > GameRules.EatLimitWindowSeconds)
            {
                CurrentProfile.PetData.eatCount = 0;
            }

            CurrentProfile.PetData.eatCount++;
            CurrentProfile.PetData.lastEatTime = Now;
            NotifyDataChanged();
        }

        public bool Brush()
        {
            if (!IsTimeToBrush)
            {
                return false;
            }

            CurrentProfile.PetData.lastBrushTime = Now;
            NotifyDataChanged();
            return true;
        }

        public void ResetPetTimes()
        {
            if (CurrentProfile?.PetData == null)
            {
                return;
            }

            CurrentProfile.PetData.lastEatTime = -1;
            CurrentProfile.PetData.eatCount = 0;
            CurrentProfile.PetData.lastBrushTime = -1;
            logger_?.Log("[PetData] Reset pet timers.");
            NotifyDataChanged();
        }

        public void SetRoomItem(int targetId, int itemId, InteractionPointType interactionPointType)
        {
            switch (interactionPointType)
            {
                case InteractionPointType.PLACEABLE_OBJECT:
                    SetRoomObject(targetId, itemId);
                    return;
                case InteractionPointType.PAINT:
                    PaintRoomSurface(targetId, itemId);
                    return;
                case InteractionPointType.EYES:
                    SetPetEyes(itemId);
                    return;
                case InteractionPointType.HAT:
                    SetPetHat(itemId);
                    return;
                case InteractionPointType.SKIN:
                    SetPetSkin(itemId);
                    return;
                case InteractionPointType.DRESS:
                    SetPetDress(itemId);
                    return;
                case InteractionPointType.FOOD:
                    FeedPet(itemId);
                    return;
            }
        }

        public void SetRoomObject(int locationId, int itemId)
        {
            if (RoomHasObject(locationId, itemId))
            {
                logger_?.LogWarning($"[RoomData] Failed to place object {itemId} at room location {locationId}.");
                NotifyRoomDataItemApplyFailed(InteractionPointType.PLACEABLE_OBJECT, itemId, locationId);
                return;
            }

            PlacedRoomObjectLocation roomObject = GetOrCreateRoomObject(locationId);
            ReturnPlacedRoomObjectToInventory(roomObject.Item);
            roomObject.Item = new PlacedRoomObject
            {
                ItemId = itemId,
                PaintId = DefaultProfileState.NoPaintItemId
            };
            logger_?.Log($"[RoomData] Placed object {itemId} at room location {locationId}.");

            ConsumeInventoryItemAndNotify(InteractionPointType.PLACEABLE_OBJECT, itemId);
            NotifyRoomDataItemApplied(InteractionPointType.PLACEABLE_OBJECT, itemId, locationId);
        }

        public void RemoveRoomObject(int locationId)
        {
            PlacedRoomObjectLocation roomObject = FindRoomObject(locationId);
            if (roomObject?.Item == null)
            {
                return;
            }

            ReturnPlacedRoomObjectToInventory(roomObject.Item);
            roomObject.Item = null;
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void MoveRoomObject(int sourceLocationId, int targetLocationId)
        {
            PlacedRoomObjectLocation sourceRoomObject = FindRoomObject(sourceLocationId);
            if (sourceRoomObject?.Item == null)
            {
                return;
            }

            PlacedRoomObject sourceItem = sourceRoomObject.Item;

            if (sourceLocationId == targetLocationId)
            {
                NotifyDataChanged();
                NotifyRoomDataItemApplied(InteractionPointType.PLACEABLE_OBJECT, sourceItem.ItemId, targetLocationId);
                return;
            }

            PlacedRoomObjectLocation targetRoomObject = GetOrCreateRoomObject(targetLocationId);
            PlacedRoomObject displacedTargetItem = targetRoomObject.Item;

            targetRoomObject.Item = sourceItem;
            sourceRoomObject.Item = displacedTargetItem;

            logger_?.Log(
                $"[RoomData] Moved object {sourceItem.ItemId} from room location {sourceLocationId} to {targetLocationId}.");
            NotifyDataChanged();
            NotifyRoomDataItemApplied(InteractionPointType.PLACEABLE_OBJECT, sourceItem.ItemId, targetLocationId);
        }

        public void ReturnRoomObjectToInventory(int sourceLocationId)
        {
            PlacedRoomObjectLocation sourceRoomObject = FindRoomObject(sourceLocationId);
            if (sourceRoomObject?.Item == null)
            {
                return;
            }

            PlacedRoomObject sourceItem = sourceRoomObject.Item;
            ReturnPlacedRoomObjectToInventory(sourceItem);
            sourceRoomObject.Item = null;
            logger_?.Log(
                $"[RoomData] Returned object {sourceItem.ItemId} from room location {sourceLocationId} to inventory.");
            NotifyInventoryChanged();
            NotifyDataChanged();
            NotifyRoomDataItemApplied(InteractionPointType.PLACEABLE_OBJECT, sourceItem.ItemId, sourceLocationId);
        }

        public void PaintRoomSurface(int surfaceId, int paintItemId)
        {
            RoomPaintSurfaceState surface = GetOrCreateRoomSurface(surfaceId);
            if (surface.PaintId == paintItemId)
            {
                logger_?.LogWarning($"[RoomData] Failed to paint surface {surfaceId} with item {paintItemId}: already applied.");
                NotifyRoomDataItemApplyFailed(InteractionPointType.PAINT, paintItemId, surfaceId);
                return;
            }

            surface.PaintId = paintItemId;
            logger_?.Log($"[RoomData] Painted surface {surfaceId} with item {paintItemId}.");
            ConsumeInventoryItemAndNotify(InteractionPointType.PAINT, paintItemId);
            NotifyRoomDataItemApplied(InteractionPointType.PAINT, paintItemId, surfaceId);
        }

        public void SetPetEyes(int itemId)
        {
            if (CurrentProfile.PetData.EyesItemId == itemId)
            {
                logger_?.LogWarning($"[RoomData] Failed to apply pet eyes item {itemId}: already applied.");
                NotifyPetDataApplyFailed(InteractionPointType.EYES, itemId);
                return;
            }

            CurrentProfile.PetData.EyesItemId = itemId;
            logger_?.Log($"[RoomData] Applied pet eyes item {itemId}.");
            ConsumeInventoryItemAndNotify(InteractionPointType.EYES, itemId);
            NotifyPetDataApplied(InteractionPointType.EYES, itemId);
        }

        public void SetPetHat(int itemId)
        {
            if (CurrentProfile.PetData.HatItemId == itemId)
            {
                logger_?.LogWarning($"[RoomData] Failed to apply pet hat item {itemId}: already applied.");
                NotifyPetDataApplyFailed(InteractionPointType.HAT, itemId);
                return;
            }

            CurrentProfile.PetData.HatItemId = itemId;
            logger_?.Log($"[RoomData] Applied pet hat item {itemId}.");
            ConsumeInventoryItemAndNotify(InteractionPointType.HAT, itemId);
            NotifyPetDataApplied(InteractionPointType.HAT, itemId);
        }

        public void SetPetSkin(int itemId)
        {
            if (CurrentProfile.PetData.SkinItemId == itemId)
            {
                logger_?.LogWarning($"[RoomData] Failed to apply pet skin item {itemId}: already applied.");
                NotifyPetDataApplyFailed(InteractionPointType.SKIN, itemId);
                return;
            }

            CurrentProfile.PetData.SkinItemId = itemId;
            logger_?.Log($"[RoomData] Applied pet skin item {itemId}.");
            ConsumeInventoryItemAndNotify(InteractionPointType.SKIN, itemId);
            NotifyPetDataApplied(InteractionPointType.SKIN, itemId);
        }

        public void SetPetDress(int itemId)
        {
            if (CurrentProfile.PetData.DressItemId == itemId)
            {
                logger_?.LogWarning($"[RoomData] Failed to apply pet dress item {itemId}: already applied.");
                NotifyPetDataApplyFailed(InteractionPointType.DRESS, itemId);
                return;
            }

            int previousDressItemId = CurrentProfile.PetData.DressItemId;
            CurrentProfile.PetData.DressItemId = itemId;

            if (itemId > 0)
            {
                CurrentProfile.InventoryData.Dress.Remove(itemId);
            }

            EnsureDefaultDressOwned();

            if (previousDressItemId > 0)
            {
                UnlockOwnedDress(previousDressItemId);
            }

            logger_?.Log($"[RoomData] Applied pet dress item {itemId}.");
            NotifyInventoryChanged();
            NotifyDataChanged();
            NotifyPetDataApplied(InteractionPointType.DRESS, itemId);
        }

        public void FeedPet(int itemId)
        {
            PetEatStatus petEatStatus = CanPetEat;
            if (petEatStatus != PetEatStatus.OK)
            {
                logger_?.LogWarning($"[Food] Failed to feed pet with item {itemId}. Reason: {petEatStatus}");
                NotifyPetDataApplyFailed(InteractionPointType.FOOD, itemId);
                return;
            }

            PetEat();
            logger_?.Log($"[Food] Successfully fed pet with item {itemId}.");
            ConsumeInventoryItemAndNotify(InteractionPointType.FOOD, itemId);
            NotifyPetDataApplied(InteractionPointType.FOOD, itemId);
        }

        public void AddPlaceableObject(int id, int quantity)
        {
            AddInventoryItem(CurrentProfile.InventoryData.PlaceableObjects, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddPaint(int id, int quantity)
        {
            AddInventoryItem(CurrentProfile.InventoryData.Paint, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddFood(int id, int quantity)
        {
            AddInventoryItem(CurrentProfile.InventoryData.Food, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddSkin(int id, int quantity)
        {
            AddInventoryItem(CurrentProfile.InventoryData.Skin, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddHat(int id, int quantity)
        {
            AddInventoryItem(CurrentProfile.InventoryData.Hat, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddDress(int id, int quantity)
        {
            if (id == DefaultProfileState.DefaultPetDressItemId)
            {
                EnsureDefaultDressOwned();
            }
            else if (quantity > 0)
            {
                UnlockOwnedDress(id);
            }

            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddEyes(int id, int quantity)
        {
            AddInventoryItem(CurrentProfile.InventoryData.Eyes, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void SetMuted(bool muted)
        {
            CurrentProfile.Muted = muted;
            NotifyDataChanged();
        }

        private void NotifyDataChanged()
        {
            dispatcher_.Send(new LocalDataChangedEvent());
        }

        private void NotifyRoomDataItemApplied(
            InteractionPointType itemType,
            int itemId,
            int targetId)
        {
            dispatcher_.Send(new RoomDataItemAppliedEvent(itemType, itemId, targetId));
        }

        private void NotifyRoomDataItemApplyFailed(
            InteractionPointType itemType,
            int itemId,
            int targetId)
        {
            dispatcher_.Send(new RoomDataItemApplyFailedEvent(itemType, itemId, targetId));
        }

        private void NotifyPetDataApplied(InteractionPointType itemType, int itemId)
        {
            dispatcher_.Send(new PetDataAppliedEvent(itemType, itemId));
        }

        private void NotifyPetDataApplyFailed(InteractionPointType itemType, int itemId)
        {
            dispatcher_.Send(new PetDataApplyFailedEvent(itemType, itemId));
        }

        private void NotifyInventoryChanged()
        {
            dispatcher_.Send(new InventoryUpdatedEvent());
        }

        private void ApplyReward(Reward reward)
        {
            if (reward == null)
            {
                return;
            }

            switch (reward.Kind)
            {
                case RewardKind.Item:
                    AddInventoryItem(reward.RewardType, reward.Id, reward.Quantity);
                    return;
                case RewardKind.Currency:
                    AddCurrency(reward.CurrencyType, reward.Quantity);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reward.Kind), reward.Kind, null);
            }
        }

        private void EnsureDefaultDressOwned()
        {
            if (CurrentProfile?.InventoryData?.Dress == null)
            {
                return;
            }

            CurrentProfile.InventoryData.Dress[DefaultProfileState.DefaultPetDressItemId] = -1;
        }

        private void UnlockOwnedDress(int itemId)
        {
            if (CurrentProfile?.InventoryData?.Dress == null || itemId <= 0)
            {
                return;
            }

            CurrentProfile.InventoryData.Dress[itemId] = -1;
        }

        private void SpendCurrency(CurrencyType currencyType, int amount)
        {
            switch (currencyType)
            {
                case CurrencyType.Coins:
                    CurrentProfile.Coins = Math.Max(0, CurrentProfile.Coins - amount);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currencyType), currencyType, null);
            }

            dispatcher_.Send(new CurrencyUpdatedEvent());
        }

        private Pet CreatePet(string name)
        {
            return new Pet
            {
                Name = name,
                lastEatTime = -1,
                eatCount = 0,
                lastBrushTime = -1,
                EyesItemId = DefaultProfileState.DefaultPetEyesItemId,
                SkinItemId = DefaultProfileState.DefaultPetSkinItemId,
                HatItemId = DefaultProfileState.DefaultPetHatItemId,
                DressItemId = DefaultProfileState.DefaultPetDressItemId
            };
        }

        private Room CreateRoom() => new Room();

        private Inventory CreateInventory() => DefaultProfileState.CreateInventory();

        private void AddInventoryItem(InteractionPointType itemType, int id, int quantity) =>
            AddInventoryItem(CurrentProfile.InventoryData.GetInventoryItems(itemType), id, quantity);

        private void AddInventoryItem(Dictionary<int, int> items, int id, int quantity)
        {
            if (items.TryGetValue(id, out int currentQuantity))
            {
                if (currentQuantity == -1)
                {
                    return;
                }

                quantity += currentQuantity;
            }

            if (quantity <= 0)
            {
                items.Remove(id);
            }
            else
            {
                items[id] = quantity;
            }
        }

        private void ConsumeInventoryItemAndNotify(InteractionPointType interactionPointType, int itemId)
        {
            AddInventoryItem(interactionPointType, itemId, -1);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        private void ReturnPlacedRoomObjectToInventory(PlacedRoomObject roomObject)
        {
            if (roomObject == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.PlaceableObjects, roomObject.ItemId, 1);
        }

        private PlacedRoomObject GetPlacedRoomObject(int locationId) => FindRoomObject(locationId)?.Item;

        private PlacedRoomObjectLocation FindRoomObject(int locationId)
        {
            List<PlacedRoomObjectLocation> roomObjects = CurrentProfile.RoomData.PlaceableObjects;

            for (int index = 0; index < roomObjects.Count; index++)
            {
                PlacedRoomObjectLocation roomObject = roomObjects[index];
                if (roomObject.LocationId == locationId)
                {
                    return roomObject;
                }
            }

            return null;
        }

        private PlacedRoomObjectLocation GetOrCreateRoomObject(int locationId)
        {
            PlacedRoomObjectLocation existingObject = FindRoomObject(locationId);
            if (existingObject != null)
            {
                return existingObject;
            }

            PlacedRoomObjectLocation newObject = new PlacedRoomObjectLocation { LocationId = locationId };
            CurrentProfile.RoomData.PlaceableObjects.Add(newObject);
            return newObject;
        }

        private RoomPaintSurfaceState FindRoomSurface(int surfaceId)
        {
            List<RoomPaintSurfaceState> paintedSurfaces = CurrentProfile.RoomData.PaintedSurfaces;

            for (int index = 0; index < paintedSurfaces.Count; index++)
            {
                RoomPaintSurfaceState surface = paintedSurfaces[index];
                if (surface.SurfaceId == surfaceId)
                {
                    return surface;
                }
            }

            return null;
        }

        private RoomPaintSurfaceState GetOrCreateRoomSurface(int surfaceId)
        {
            RoomPaintSurfaceState existingSurface = FindRoomSurface(surfaceId);
            if (existingSurface != null)
            {
                return existingSurface;
            }

            RoomPaintSurfaceState newSurface = new RoomPaintSurfaceState
            {
                SurfaceId = surfaceId,
                PaintId = DefaultProfileState.NoPaintItemId
            };

            CurrentProfile.RoomData.PaintedSurfaces.Add(newSurface);
            return newSurface;
        }
    }
}
