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
    /// Transitional gameplay facade over the client state store.
    /// Remote-backed areas are being extracted into dedicated services over time.
    /// </summary>
    public class DataRepository
    {
        private readonly EventDispatcher dispatcher_;
        private readonly ClientGameStateStore gameStateStore_;
        private readonly IProfileService profileService_;
        private readonly IRoomGameplayService roomGameplayService_;

        public GameState Data => gameStateStore_.Data;

        public Profile CurrentProfile => gameStateStore_.CurrentProfile;

        public List<Profile> AllProfiles => gameStateStore_.AllProfiles;

        public DataRepository(
            ClientGameStateStore gameStateStore,
            IProfileService profileService,
            IRoomGameplayService roomGameplayService,
            EventDispatcher dispatcher)
        {
            gameStateStore_ = gameStateStore ?? throw new ArgumentNullException(nameof(gameStateStore));
            profileService_ = profileService ?? throw new ArgumentNullException(nameof(profileService));
            roomGameplayService_ = roomGameplayService ?? throw new ArgumentNullException(nameof(roomGameplayService));
            dispatcher_ = dispatcher;
        }

        public bool CanUseName(string name) => profileService_.CanUseName(name);

        public bool CanUsePetName(string name) => profileService_.CanUsePetName(name);

        public Profile CreateProfile(string name, string petName, int pictureId) =>
            profileService_.CreateProfile(name, petName, pictureId);

        public void ModifyCurrentProfile(string name, string petName, int pictureId) =>
            profileService_.ModifyCurrentProfile(name, petName, pictureId);

        public void SetPendingReward(bool pendingReward)
        {
            if (CurrentProfile == null)
            {
                return;
            }

            CurrentProfile.PendingReward = pendingReward;
            dispatcher_?.Send(new PendingRewardEvent());
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

            dispatcher_?.Send(new CurrencyUpdatedEvent());
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
                dispatcher_?.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.ITEM_NOT_FOUND, null));
                return MarketPurchaseStatus.ITEM_NOT_FOUND;
            }

            if (IsMarketItemAlreadyOwned(itemType, itemId))
            {
                dispatcher_?.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.ALREADY_OWNED, itemDefinition));
                return MarketPurchaseStatus.ALREADY_OWNED;
            }

            Dictionary<int, int> items = CurrentProfile.InventoryData.GetInventoryItems(itemType);
            if (items.TryGetValue(itemId, out int currentQuantity) && currentQuantity == -1)
            {
                dispatcher_?.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.ALREADY_OWNED, itemDefinition));
                return MarketPurchaseStatus.ALREADY_OWNED;
            }

            if (!CanAfford(itemDefinition.CurrencyType, itemDefinition.Price))
            {
                dispatcher_?.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.NOT_ENOUGH_CURRENCY, itemDefinition));
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
            dispatcher_?.Send(new MarketPurchaseCompletedEvent(MarketPurchaseStatus.OK, itemDefinition));
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

            dispatcher_?.Send(new ProfileUpdatedEvent());
            NotifyDataChanged();
        }

        public PetEatStatus CanPetEat => roomGameplayService_.CanPetEat;

        public bool IsTimeToBrush => roomGameplayService_.IsTimeToBrush;

        public bool RoomHasObject(int locationId, int itemId) => roomGameplayService_.RoomHasObject(locationId, itemId);

        public bool RoomSurfaceHasPaint(int surfaceId, int paintItemId) =>
            roomGameplayService_.RoomSurfaceHasPaint(surfaceId, paintItemId);

        public int GetRoomSurfacePaintId(int surfaceId) => roomGameplayService_.GetRoomSurfacePaintId(surfaceId);

        public int GetAppliedPetItemId(InteractionPointType interactionPointType) =>
            roomGameplayService_.GetAppliedPetItemId(interactionPointType);

        public bool IsMarketItemAlreadyOwned(InteractionPointType itemType, int itemId) =>
            roomGameplayService_.IsMarketItemAlreadyOwned(itemType, itemId);

        public void PetEat() => roomGameplayService_.PetEat();

        public bool Brush() => roomGameplayService_.Brush();

        public void ResetPetTimes() => roomGameplayService_.ResetPetTimes();

        public void SetRoomItem(int targetId, int itemId, InteractionPointType interactionPointType) =>
            roomGameplayService_.SetRoomItem(targetId, itemId, interactionPointType);

        public void SetRoomObject(int locationId, int itemId) => roomGameplayService_.SetRoomObject(locationId, itemId);

        public void RemoveRoomObject(int locationId) => roomGameplayService_.RemoveRoomObject(locationId);

        public void MoveRoomObject(int sourceLocationId, int targetLocationId) =>
            roomGameplayService_.MoveRoomObject(sourceLocationId, targetLocationId);

        public void ReturnRoomObjectToInventory(int sourceLocationId) =>
            roomGameplayService_.ReturnRoomObjectToInventory(sourceLocationId);

        public void PaintRoomSurface(int surfaceId, int paintItemId) =>
            roomGameplayService_.PaintRoomSurface(surfaceId, paintItemId);

        public void SetPetEyes(int itemId) => roomGameplayService_.SetPetEyes(itemId);

        public void SetPetHat(int itemId) => roomGameplayService_.SetPetHat(itemId);

        public void SetPetSkin(int itemId) => roomGameplayService_.SetPetSkin(itemId);

        public void SetPetDress(int itemId) => roomGameplayService_.SetPetDress(itemId);

        public void FeedPet(int itemId) => roomGameplayService_.FeedPet(itemId);

        public void AddPlaceableObject(int id, int quantity) => roomGameplayService_.AddPlaceableObject(id, quantity);

        public void AddPaint(int id, int quantity) => roomGameplayService_.AddPaint(id, quantity);

        public void AddFood(int id, int quantity) => roomGameplayService_.AddFood(id, quantity);

        public void AddSkin(int id, int quantity) => roomGameplayService_.AddSkin(id, quantity);

        public void AddHat(int id, int quantity) => roomGameplayService_.AddHat(id, quantity);

        public void AddDress(int id, int quantity) => roomGameplayService_.AddDress(id, quantity);

        public void AddEyes(int id, int quantity) => roomGameplayService_.AddEyes(id, quantity);

        public void SetMuted(bool muted) => roomGameplayService_.SetMuted(muted);

        private void NotifyDataChanged()
        {
            dispatcher_?.Send(new LocalDataChangedEvent());
            dispatcher_?.Send(new ChildGameStateLocallyChangedEvent());
        }

        private void NotifyInventoryChanged()
        {
            dispatcher_?.Send(new InventoryUpdatedEvent());
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

            dispatcher_?.Send(new CurrencyUpdatedEvent());
        }

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
    }
}
