using System;
using System.Collections.Generic;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Rules;

namespace Game.Core.Services
{
    /// <summary>
    /// Implements room, pet, and inventory gameplay mutations for the active profile.
    /// </summary>
    public sealed class RoomGameplayService : IRoomGameplayService
    {
        private readonly ClientGameStateStore gameStateStore_;
        private readonly EventDispatcher dispatcher_;
        private readonly IGameLogger logger_;

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public RoomGameplayService(
            ClientGameStateStore gameStateStore,
            EventDispatcher dispatcher,
            IGameLogger logger)
        {
            gameStateStore_ = gameStateStore ?? throw new ArgumentNullException(nameof(gameStateStore));
            dispatcher_ = dispatcher;
            logger_ = logger;
        }

        private Profile CurrentProfile => gameStateStore_.CurrentProfile;

        public PetEatStatus CanPetEat
        {
            get
            {
                if (CurrentProfile?.PetData == null)
                {
                    return PetEatStatus.NO_MORE;
                }

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

        public bool IsTimeToBrush =>
            CurrentProfile?.PetData != null &&
            Now - CurrentProfile.PetData.lastBrushTime > GameRules.BrushCooldownSeconds;

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
            if (CurrentProfile?.PetData == null)
            {
                return;
            }

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
            if (!IsTimeToBrush || CurrentProfile?.PetData == null)
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
            if (CurrentProfile?.RoomData == null)
            {
                return;
            }

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
            if (CurrentProfile?.RoomData == null)
            {
                return;
            }

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
            if (CurrentProfile?.PetData == null)
            {
                return;
            }

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
            if (CurrentProfile?.PetData == null)
            {
                return;
            }

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
            if (CurrentProfile?.PetData == null)
            {
                return;
            }

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
            if (CurrentProfile?.PetData == null || CurrentProfile?.InventoryData == null)
            {
                return;
            }

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
            if (CurrentProfile?.InventoryData == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.PlaceableObjects, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddPaint(int id, int quantity)
        {
            if (CurrentProfile?.InventoryData == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.Paint, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddFood(int id, int quantity)
        {
            if (CurrentProfile?.InventoryData == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.Food, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddSkin(int id, int quantity)
        {
            if (CurrentProfile?.InventoryData == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.Skin, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddHat(int id, int quantity)
        {
            if (CurrentProfile?.InventoryData == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.Hat, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void AddDress(int id, int quantity)
        {
            if (CurrentProfile?.InventoryData == null)
            {
                return;
            }

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
            if (CurrentProfile?.InventoryData == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.Eyes, id, quantity);
            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        public void SetMuted(bool muted)
        {
            if (CurrentProfile == null)
            {
                return;
            }

            CurrentProfile.Muted = muted;
            NotifyDataChanged();
        }

        private void NotifyDataChanged()
        {
            dispatcher_?.Send(new LocalDataChangedEvent());
            dispatcher_?.Send(new ChildGameStateLocallyChangedEvent());
        }

        private void NotifyRoomDataItemApplied(InteractionPointType itemType, int itemId, int targetId)
        {
            dispatcher_?.Send(new RoomDataItemAppliedEvent(itemType, itemId, targetId));
        }

        private void NotifyRoomDataItemApplyFailed(InteractionPointType itemType, int itemId, int targetId)
        {
            dispatcher_?.Send(new RoomDataItemApplyFailedEvent(itemType, itemId, targetId));
        }

        private void NotifyPetDataApplied(InteractionPointType itemType, int itemId)
        {
            dispatcher_?.Send(new PetDataAppliedEvent(itemType, itemId));
        }

        private void NotifyPetDataApplyFailed(InteractionPointType itemType, int itemId)
        {
            dispatcher_?.Send(new PetDataApplyFailedEvent(itemType, itemId));
        }

        private void NotifyInventoryChanged()
        {
            dispatcher_?.Send(new InventoryUpdatedEvent());
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

        private void AddInventoryItem(InteractionPointType itemType, int id, int quantity) =>
            AddInventoryItem(CurrentProfile.InventoryData.GetInventoryItems(itemType), id, quantity);

        private static void AddInventoryItem(Dictionary<int, int> items, int id, int quantity)
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
            if (CurrentProfile?.InventoryData == null)
            {
                return;
            }

            Dictionary<int, int> items = CurrentProfile.InventoryData.GetInventoryItems(interactionPointType);
            if (!items.TryGetValue(itemId, out int currentQuantity))
            {
                logger_?.LogWarning($"[Inventory] Missing item {itemId} in {interactionPointType}; cannot consume.");
                return;
            }

            if (currentQuantity == -1)
            {
                logger_?.Log($"[Inventory] Item {itemId} in {interactionPointType} is permanently owned; no quantity consumed.");
                NotifyDataChanged();
                return;
            }

            int newQuantity = currentQuantity - 1;
            if (newQuantity <= 0)
            {
                items.Remove(itemId);
                logger_?.Log($"[Inventory] Consumed item {itemId} in {interactionPointType}. Removed from inventory.");
            }
            else
            {
                items[itemId] = newQuantity;
                logger_?.Log($"[Inventory] Consumed item {itemId} in {interactionPointType}. Remaining quantity: {newQuantity}.");
            }

            NotifyInventoryChanged();
            NotifyDataChanged();
        }

        private void ReturnPlacedRoomObjectToInventory(PlacedRoomObject roomObject)
        {
            if (roomObject == null || CurrentProfile?.InventoryData == null)
            {
                return;
            }

            AddInventoryItem(CurrentProfile.InventoryData.PlaceableObjects, roomObject.ItemId, 1);

            if (roomObject.PaintId > DefaultProfileState.NoPaintItemId)
            {
                AddInventoryItem(CurrentProfile.InventoryData.Paint, roomObject.PaintId, 1);
            }
        }

        private PlacedRoomObjectLocation GetOrCreateRoomObject(int locationId)
        {
            if (CurrentProfile?.RoomData == null)
            {
                CurrentProfile.RoomData = new Room();
            }

            PlacedRoomObjectLocation roomObject = FindRoomObject(locationId);
            if (roomObject != null)
            {
                return roomObject;
            }

            roomObject = new PlacedRoomObjectLocation
            {
                LocationId = locationId
            };
            CurrentProfile.RoomData.PlaceableObjects.Add(roomObject);
            return roomObject;
        }

        private PlacedRoomObjectLocation FindRoomObject(int locationId)
        {
            if (CurrentProfile?.RoomData?.PlaceableObjects == null)
            {
                return null;
            }

            for (int index = 0; index < CurrentProfile.RoomData.PlaceableObjects.Count; index++)
            {
                PlacedRoomObjectLocation roomObject = CurrentProfile.RoomData.PlaceableObjects[index];
                if (roomObject != null && roomObject.LocationId == locationId)
                {
                    return roomObject;
                }
            }

            return null;
        }

        private RoomPaintSurfaceState GetOrCreateRoomSurface(int surfaceId)
        {
            if (CurrentProfile?.RoomData == null)
            {
                CurrentProfile.RoomData = new Room();
            }

            RoomPaintSurfaceState surface = FindRoomSurface(surfaceId);
            if (surface != null)
            {
                return surface;
            }

            surface = new RoomPaintSurfaceState
            {
                SurfaceId = surfaceId,
                PaintId = DefaultProfileState.NoPaintItemId
            };
            CurrentProfile.RoomData.PaintedSurfaces.Add(surface);
            return surface;
        }

        private RoomPaintSurfaceState FindRoomSurface(int surfaceId)
        {
            if (CurrentProfile?.RoomData?.PaintedSurfaces == null)
            {
                return null;
            }

            for (int index = 0; index < CurrentProfile.RoomData.PaintedSurfaces.Count; index++)
            {
                RoomPaintSurfaceState surface = CurrentProfile.RoomData.PaintedSurfaces[index];
                if (surface != null && surface.SurfaceId == surfaceId)
                {
                    return surface;
                }
            }

            return null;
        }
    }
}
