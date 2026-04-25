using System;
using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Services;

using UnityEngine;

using Zenject;

namespace Game.Unity.Development
{
    /// <summary>
    /// Editor/development-only bootstrap that guarantees a usable test profile exists.
    /// </summary>
    public sealed class DevelopmentProfileBootstrap : IInitializable
    {
        private readonly DataRepository repository_;
        private readonly DevelopmentProfileSettings settings_;

        public DevelopmentProfileBootstrap(DataRepository repository, DevelopmentProfileSettings settings)
        {
            repository_ = repository;
            settings_ = settings;
        }

        public void Initialize()
        {
            if (!IsEnabled() || !ShouldRun())
            {
                return;
            }

            EnsureProfile();
            EnsureMinimumCoins();
            ResetPetTimesIfNeeded();
            EnsureInventory();
        }

        private bool ShouldRun()
        {
            if (!OnlyInEditorOrDevelopmentBuild())
            {
                return true;
            }

            return Application.isEditor || Debug.isDebugBuild;
        }

        private void EnsureProfile()
        {
            int existingIndex = FindProfileIndex(ProfileName());

            if (existingIndex >= 0)
            {
                repository_.SwitchProfile(existingIndex);
                return;
            }

            if (repository_.CurrentProfile == null && repository_.AllProfiles.Count > 0)
            {
                repository_.SwitchProfile(0);
                return;
            }

            if (repository_.CurrentProfile != null)
            {
                return;
            }

            string profileName = string.IsNullOrWhiteSpace(ProfileName())
                ? "Dev Kid"
                : ProfileName().Trim();

            string petName = string.IsNullOrWhiteSpace(PetName())
                ? "Brushy"
                : PetName().Trim();

            int pictureId = Mathf.Clamp(ProfilePictureId(), 1, GameIds.ProfilePictureCount);
            repository_.CreateProfile(profileName, petName, pictureId);
        }

        private void EnsureMinimumCoins()
        {
            if (repository_.CurrentProfile == null || MinimumCoins() <= 0)
            {
                return;
            }

            int currentCoins = repository_.GetCurrencyBalance(CurrencyType.Coins);
            int delta = MinimumCoins() - currentCoins;

            if (delta > 0)
            {
                repository_.AddCurrency(CurrencyType.Coins, delta);
            }
        }

        private int FindProfileIndex(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return -1;
            }

            List<Profile> profiles = repository_.AllProfiles;
            for (int index = 0; index < profiles.Count; index++)
            {
                if (string.Equals(profiles[index].Name, profileName, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private void EnsureInventory()
        {
            if (repository_.CurrentProfile?.InventoryData == null || settings_ == null)
            {
                return;
            }

            EnsureInventoryCategory(
                InteractionPointType.PLACEABLE_OBJECT,
                repository_.CurrentProfile.InventoryData.PlaceableObjects,
                settings_.PlaceableObjects,
                repository_.AddPlaceableObject);

            EnsureInventoryCategory(
                InteractionPointType.PAINT,
                repository_.CurrentProfile.InventoryData.Paint,
                settings_.Paint,
                repository_.AddPaint);

            EnsureInventoryCategory(
                InteractionPointType.FOOD,
                repository_.CurrentProfile.InventoryData.Food,
                settings_.Food,
                repository_.AddFood);

            EnsureInventoryCategory(
                InteractionPointType.HAT,
                repository_.CurrentProfile.InventoryData.Hat,
                settings_.Hat,
                repository_.AddHat);

            EnsureInventoryCategory(
                InteractionPointType.SKIN,
                repository_.CurrentProfile.InventoryData.Skin,
                settings_.Skin,
                repository_.AddSkin);

            EnsureInventoryCategory(
                InteractionPointType.DRESS,
                repository_.CurrentProfile.InventoryData.Dress,
                settings_.Dress,
                repository_.AddDress);

            EnsureInventoryCategory(
                InteractionPointType.EYES,
                repository_.CurrentProfile.InventoryData.Eyes,
                settings_.Eyes,
                repository_.AddEyes);
        }

        private void ResetPetTimesIfNeeded()
        {
            if (repository_.CurrentProfile?.PetData == null || settings_ == null)
            {
                return;
            }

            if (!ShouldResetPetTimesOnStartup())
            {
                return;
            }

            repository_.ResetPetTimes();
        }

        private bool ShouldResetPetTimesOnStartup()
        {
            foreach (InteractionPointType interactionPointType in Enum.GetValues(typeof(InteractionPointType)))
            {
                DevelopmentInventoryCategorySettings categorySettings = settings_.GetCategorySettings(interactionPointType);
                if (categorySettings != null && categorySettings.ResetPetTimesOnStartup)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureInventoryCategory(
            InteractionPointType interactionPointType,
            Dictionary<int, int> inventoryItems,
            DevelopmentInventoryCategorySettings categorySettings,
            Action<int, int> addItem)
        {
            if (inventoryItems == null || categorySettings == null || addItem == null)
            {
                return;
            }

            int itemCount = ItemCatalog.Get(interactionPointType).ItemCount;
            int firstItemCount = Mathf.Clamp(categorySettings.FirstItemCount, 0, itemCount);

            for (int itemId = 1; itemId <= firstItemCount; itemId++)
            {
                EnsureInventoryItem(inventoryItems, itemId, 1, addItem);
            }

            DevelopmentInventoryEntry[] additionalItems = categorySettings.AdditionalItems;
            if (additionalItems == null)
            {
                return;
            }

            for (int index = 0; index < additionalItems.Length; index++)
            {
                DevelopmentInventoryEntry entry = additionalItems[index];
                if (entry == null || entry.ItemId <= 0 || entry.ItemId > itemCount || entry.Quantity <= 0)
                {
                    continue;
                }

                EnsureInventoryItem(inventoryItems, entry.ItemId, entry.Quantity, addItem);
            }
        }

        private static void EnsureInventoryItem(
            Dictionary<int, int> inventoryItems,
            int itemId,
            int minimumQuantity,
            Action<int, int> addItem)
        {
            if (inventoryItems.TryGetValue(itemId, out int currentQuantity))
            {
                if (currentQuantity == -1)
                {
                    return;
                }

                int delta = minimumQuantity - currentQuantity;
                if (delta > 0)
                {
                    addItem(itemId, delta);
                }

                return;
            }

            addItem(itemId, minimumQuantity);
        }

        private bool IsEnabled() => settings_ == null || settings_.Enabled;

        private bool OnlyInEditorOrDevelopmentBuild() => settings_ == null || settings_.OnlyInEditorOrDevelopmentBuild;

        private string ProfileName() => settings_ != null ? settings_.ProfileName : "Dev Kid";

        private string PetName() => settings_ != null ? settings_.PetName : "Brushy";

        private int ProfilePictureId() => settings_ != null ? settings_.ProfilePictureId : 1;

        private int MinimumCoins() => settings_ != null ? settings_.MinimumCoins : 250;
    }
}
