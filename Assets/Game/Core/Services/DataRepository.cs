using System;
using System.Collections.Generic;

using Flowbit.Utilities.Core.Events;

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

        public GameState Data { get; private set; }

        public Profile CurrentProfile { get; private set; }

        public List<Profile> AllProfiles => Data.Profiles;

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public DataRepository(GameState data, EventDispatcher dispatcher)
        {
            if (data == null)
            {
                data = new GameState();
            }

            dispatcher_ = dispatcher;
            Data = data;

            if (Data.CurrentProfile != -1)
            {
                CurrentProfile = Data.Profiles[Data.CurrentProfile];
            }
        }

        public bool CanUseName(string name) =>
            (CurrentProfile != null && name == CurrentProfile.Name) ||
            (name != "" && !Data.Profiles.Exists(item => item.Name == name));

        public bool CanUsePetName(string name) => name != "";

        public Profile CreateProfile(string name, string petName, int pictureId)
        {
            if (!CanUseName(name))
            {
                dispatcher_.Send(new DataSavedEvent(DataSaveStatus.NAME_EXIST));
                return null;
            }

            Profile profile = new Profile
            {
                Name = name,
                ProfilePictureId = pictureId,
                PetData = CreatePet(petName),
                RoomData = CreateRoom(),
                InventoryData = CreateInventory(),
                PendingReward = DefaultProfileState.InitialPendingReward
            };

            Data.Profiles.Add(profile);
            SwitchProfile(Data.Profiles.Count - 1);
            return profile;
        }

        public void ModifyCurrentProfile(string name, string petName, int pictureId)
        {
            if (!CanUseName(name))
            {
                dispatcher_.Send(new DataSavedEvent(DataSaveStatus.NAME_EXIST));
                return;
            }

            if (CurrentProfile == null)
            {
                dispatcher_.Send(new DataSavedEvent(DataSaveStatus.NO_CURRENT_PROFILE));
                return;
            }

            CurrentProfile.Name = name;
            CurrentProfile.PetData.Name = petName;
            CurrentProfile.ProfilePictureId = pictureId;
            dispatcher_.Send(new ProfileUpdatedEvent());
            NotifyDataChanged();
        }

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
            AddInventoryItem(itemType, itemId, itemDefinition.Quantity);
            NotifyInventoryChanged();
            dispatcher_.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.OK, itemDefinition));
            NotifyDataChanged();
            return MarketPurchaseStatus.OK;
        }

        public Reward[] GiveRewards()
        {
            SetPendingReward(false);
            Reward[] rewards = new Reward[2];

            rewards[0] = GameRules.GetRandomReward(InteractionPointType.FOOD);
            rewards[1] = GameRules.GetRandomReward();

            foreach (Reward reward in rewards)
            {
                AddInventoryItem(reward.RewardType, reward.Id, reward.Quantity);
            }

            NotifyDataChanged();
            NotifyInventoryChanged();
            return rewards;
        }

        public void SwitchProfile(int index)
        {
            CurrentProfile = Data.Profiles[index];
            Data.CurrentProfile = index;
            NotifyDataChanged();
            dispatcher_.Send(new ProfileSwitchedEvent());
        }

        public void SetPetName(string name)
        {
            CurrentProfile.PetData.Name = name;
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

        public bool RoomChildSlotContainsObject(int locationId, int slotId, int itemId)
        {
            PlacedChildObjectSlot slot = FindRoomChildSlot(locationId, slotId);
            return slot?.Item != null && slot.Item.ItemId == itemId;
        }

        public bool RoomSurfaceHasPaint(int surfaceId, int paintItemId)
        {
            RoomPaintSurfaceState surface = FindRoomSurface(surfaceId);
            return surface != null && surface.PaintId == paintItemId;
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
                case InteractionPointType.FACE:
                    SetPetFace(itemId);
                    return;
                case InteractionPointType.SKIN:
                    SetPetSkin(itemId);
                    return;
                case InteractionPointType.FOOD:
                    FeedPet(itemId);
                    return;
            }
        }

        public void SetRoomObject(int locationId, int itemId)
        {
            if (!GameIds.IsRoomObjectLocationId(locationId) || RoomHasObject(locationId, itemId))
            {
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

        public void SetChildRoomObject(int locationId, int slotId, int itemId)
        {
            PlacedRoomObject roomObject = GetPlacedRoomObject(locationId);
            if (roomObject == null)
            {
                NotifyRoomDataItemApplyFailed(InteractionPointType.PLACEABLE_OBJECT, itemId, slotId, locationId);
                return;
            }

            PlacedChildObjectSlot slot = GetOrCreateRoomChildSlot(locationId, slotId);
            if (slot.Item != null && slot.Item.ItemId == itemId)
            {
                NotifyRoomDataItemApplyFailed(InteractionPointType.PLACEABLE_OBJECT, itemId, slotId, locationId);
                return;
            }

            if (slot.Item != null)
            {
                AddInventoryItem(CurrentProfile.InventoryData.PlaceableObjects, slot.Item.ItemId, 1);
            }

            slot.Item = new PlacedChildRoomObject
            {
                ItemId = itemId,
                PaintId = DefaultProfileState.NoPaintItemId
            };

            ConsumeInventoryItemAndNotify(InteractionPointType.PLACEABLE_OBJECT, itemId);
            NotifyRoomDataItemApplied(InteractionPointType.PLACEABLE_OBJECT, itemId, slotId, locationId);
        }

        public void RemoveChildRoomObject(int locationId, int slotId)
        {
            PlacedChildObjectSlot slot = FindRoomChildSlot(locationId, slotId);
            if (slot?.Item == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.PlaceableObjects, slot.Item.ItemId, 1);
            slot.Item = null;
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void PaintRoomSurface(int surfaceId, int paintItemId)
        {
            if (!GameIds.IsRoomSurfaceId(surfaceId))
            {
                NotifyRoomDataItemApplyFailed(InteractionPointType.PAINT, paintItemId, surfaceId);
                return;
            }

            RoomPaintSurfaceState surface = GetOrCreateRoomSurface(surfaceId);
            if (surface.PaintId == paintItemId)
            {
                NotifyRoomDataItemApplyFailed(InteractionPointType.PAINT, paintItemId, surfaceId);
                return;
            }

            surface.PaintId = paintItemId;
            ConsumeInventoryItemAndNotify(InteractionPointType.PAINT, paintItemId);
            NotifyRoomDataItemApplied(InteractionPointType.PAINT, paintItemId, surfaceId);
        }

        public void PaintRoomObject(int locationId, int paintItemId)
        {
            PlacedRoomObject roomObject = GetPlacedRoomObject(locationId);
            if (roomObject == null)
            {
                NotifyRoomDataItemApplyFailed(InteractionPointType.PAINT, paintItemId, locationId);
                return;
            }

            if (roomObject.PaintId == paintItemId)
            {
                NotifyRoomDataItemApplyFailed(InteractionPointType.PAINT, paintItemId, locationId);
                return;
            }

            roomObject.PaintId = paintItemId;
            ConsumeInventoryItemAndNotify(InteractionPointType.PAINT, paintItemId);
            NotifyRoomDataItemApplied(InteractionPointType.PAINT, paintItemId, locationId);
        }

        public void PaintChildRoomObject(int locationId, int slotId, int paintItemId)
        {
            PlacedChildObjectSlot slot = FindRoomChildSlot(locationId, slotId);
            if (slot?.Item == null)
            {
                NotifyRoomDataItemApplyFailed(InteractionPointType.PAINT, paintItemId, slotId, locationId);
                return;
            }

            if (slot.Item.PaintId == paintItemId)
            {
                NotifyRoomDataItemApplyFailed(InteractionPointType.PAINT, paintItemId, slotId, locationId);
                return;
            }

            slot.Item.PaintId = paintItemId;
            ConsumeInventoryItemAndNotify(InteractionPointType.PAINT, paintItemId);
            NotifyRoomDataItemApplied(InteractionPointType.PAINT, paintItemId, slotId, locationId);
        }

        public void SetPetFace(int itemId)
        {
            if (CurrentProfile.PetData.FaceItemId == itemId)
            {
                NotifyPetDataApplyFailed(InteractionPointType.FACE, itemId);
                return;
            }

            CurrentProfile.PetData.FaceItemId = itemId;
            ConsumeInventoryItemAndNotify(InteractionPointType.FACE, itemId);
            NotifyPetDataApplied(InteractionPointType.FACE, itemId);
        }

        public void SetPetSkin(int itemId)
        {
            if (CurrentProfile.PetData.SkinItemId == itemId)
            {
                NotifyPetDataApplyFailed(InteractionPointType.SKIN, itemId);
                return;
            }

            CurrentProfile.PetData.SkinItemId = itemId;
            ConsumeInventoryItemAndNotify(InteractionPointType.SKIN, itemId);
            NotifyPetDataApplied(InteractionPointType.SKIN, itemId);
        }

        public void FeedPet(int itemId)
        {
            if (CanPetEat != PetEatStatus.OK)
            {
                return;
            }

            PetEat();
            ConsumeInventoryItemAndNotify(InteractionPointType.FOOD, itemId);
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

        public void AddFace(int id, int quantity)
        {
            AddInventoryItem(CurrentProfile.InventoryData.Face, id, quantity);
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
            int targetId,
            int parentTargetId = -1)
        {
            dispatcher_.Send(new RoomDataItemAppliedEvent(itemType, itemId, targetId, parentTargetId));
        }

        private void NotifyRoomDataItemApplyFailed(
            InteractionPointType itemType,
            int itemId,
            int targetId,
            int parentTargetId = -1)
        {
            dispatcher_.Send(new RoomDataItemApplyFailedEvent(itemType, itemId, targetId, parentTargetId));
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
                FaceItemId = DefaultProfileState.DefaultPetFaceItemId,
                SkinItemId = DefaultProfileState.DefaultPetSkinItemId
            };
        }

        private Room CreateRoom() => DefaultProfileState.CreateRoom();

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

            for (int index = 0; index < roomObject.ChildObjects.Count; index++)
            {
                PlacedChildRoomObject childObject = roomObject.ChildObjects[index].Item;
                if (childObject == null)
                {
                    continue;
                }

                AddInventoryItem(CurrentProfile.InventoryData.PlaceableObjects, childObject.ItemId, 1);
            }
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

        private PlacedChildObjectSlot FindRoomChildSlot(int locationId, int slotId)
        {
            PlacedRoomObject roomObject = GetPlacedRoomObject(locationId);
            if (roomObject == null)
            {
                return null;
            }

            for (int index = 0; index < roomObject.ChildObjects.Count; index++)
            {
                PlacedChildObjectSlot slot = roomObject.ChildObjects[index];
                if (slot.SlotId == slotId)
                {
                    return slot;
                }
            }

            return null;
        }

        private PlacedChildObjectSlot GetOrCreateRoomChildSlot(int locationId, int slotId)
        {
            PlacedChildObjectSlot existingSlot = FindRoomChildSlot(locationId, slotId);
            if (existingSlot != null)
            {
                return existingSlot;
            }

            PlacedChildObjectSlot newSlot = new PlacedChildObjectSlot { SlotId = slotId };
            GetPlacedRoomObject(locationId).ChildObjects.Add(newSlot);
            return newSlot;
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
