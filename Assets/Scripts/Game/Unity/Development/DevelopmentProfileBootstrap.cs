using System;
using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Rules;
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
        private readonly IRoomGameplayService roomGameplayService_;
        private readonly DevelopmentBootstrapSettings settings_;

        public DevelopmentProfileBootstrap(
            DataRepository repository,
            IRoomGameplayService roomGameplayService,
            DevelopmentBootstrapSettings settings)
        {
            repository_ = repository;
            roomGameplayService_ = roomGameplayService;
            settings_ = settings;
        }

        public void Initialize()
        {
            GameRules.SetForcePlaceableObjectRewardsOnly(
                settings_ != null &&
                settings_.ForcePlaceableObjectRewardsOnly &&
                ShouldRun());

            if (!IsEnabled() || !ShouldRun())
            {
                return;
            }

            List<int> profileIndices = EnsureProfiles();
            if (profileIndices.Count == 0)
            {
                return;
            }

            int restoreProfileIndex = repository_.CurrentProfile != null
                ? repository_.AllProfiles.IndexOf(repository_.CurrentProfile)
                : profileIndices[0];

            for (int index = 0; index < profileIndices.Count; index++)
            {
                repository_.SwitchProfile(profileIndices[index]);
                EnsureMinimumCoins();
                ResetPetTimesIfNeeded();
                EnsureInventory();
            }

            if (restoreProfileIndex >= 0 && restoreProfileIndex < repository_.AllProfiles.Count)
            {
                repository_.SwitchProfile(restoreProfileIndex);
            }
            else
            {
                repository_.SwitchProfile(profileIndices[0]);
            }
        }

        private bool ShouldRun()
        {
            if (!OnlyInEditorOrDevelopmentBuild())
            {
                return true;
            }

            return Application.isEditor || Debug.isDebugBuild;
        }

        private List<int> EnsureProfiles()
        {
            List<int> ensuredProfileIndices = new List<int>();
            int targetProfileCount = Mathf.Max(1, TestProfileCount());

            for (int profileIndex = 0; profileIndex < targetProfileCount; profileIndex++)
            {
                string profileName = GetProfileName(profileIndex, targetProfileCount);
                int existingIndex = FindProfileIndex(profileName);
                if (existingIndex >= 0)
                {
                    ensuredProfileIndices.Add(existingIndex);
                    continue;
                }

                string petName = GetPetName(profileIndex, targetProfileCount);
                int pictureId = Mathf.Clamp(ProfilePictureId(), 1, 10);
                repository_.CreateProfile(profileName, petName, pictureId);
                ensuredProfileIndices.Add(repository_.AllProfiles.Count - 1);
            }

            if (ensuredProfileIndices.Count == 0 &&
                repository_.CurrentProfile == null &&
                repository_.AllProfiles.Count > 0)
            {
                repository_.SwitchProfile(0);
            }

            return ensuredProfileIndices;
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
                roomGameplayService_.AddPlaceableObject);

            EnsureInventoryCategory(
                InteractionPointType.PAINT,
                repository_.CurrentProfile.InventoryData.Paint,
                settings_.Paint,
                roomGameplayService_.AddPaint);

            EnsureInventoryCategory(
                InteractionPointType.FOOD,
                repository_.CurrentProfile.InventoryData.Food,
                settings_.Food,
                roomGameplayService_.AddFood);

            EnsureInventoryCategory(
                InteractionPointType.HAT,
                repository_.CurrentProfile.InventoryData.Hat,
                settings_.Hat,
                roomGameplayService_.AddHat);

            EnsureInventoryCategory(
                InteractionPointType.SKIN,
                repository_.CurrentProfile.InventoryData.Skin,
                settings_.Skin,
                roomGameplayService_.AddSkin);

            EnsureInventoryCategory(
                InteractionPointType.DRESS,
                repository_.CurrentProfile.InventoryData.Dress,
                settings_.Dress,
                roomGameplayService_.AddDress);

            EnsureInventoryCategory(
                InteractionPointType.EYES,
                repository_.CurrentProfile.InventoryData.Eyes,
                settings_.Eyes,
                roomGameplayService_.AddEyes);
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

            roomGameplayService_.ResetPetTimes();
        }

        private bool ShouldResetPetTimesOnStartup()
        {
            foreach (InteractionPointType interactionPointType in Enum.GetValues(typeof(InteractionPointType)))
            {
                DevelopmentBootstrapInventoryCategorySettings categorySettings = settings_.GetCategorySettings(interactionPointType);
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
            DevelopmentBootstrapInventoryCategorySettings categorySettings,
            Action<int, int> addItem)
        {
            if (inventoryItems == null || categorySettings == null || addItem == null)
            {
                return;
            }

            ItemCategoryConfig itemConfig = ItemCatalog.Get(interactionPointType);
            int firstItemCount = Mathf.Clamp(categorySettings.FirstItemCount, 0, itemConfig.ValidItemIds.Count);

            for (int index = 0; index < firstItemCount; index++)
            {
                int itemId = itemConfig.ValidItemIds[index];
                EnsureInventoryItem(inventoryItems, itemId, 1, addItem);
            }

            DevelopmentBootstrapInventoryEntry[] additionalItems = categorySettings.AdditionalItems;
            if (additionalItems == null)
            {
                return;
            }

            for (int index = 0; index < additionalItems.Length; index++)
            {
                DevelopmentBootstrapInventoryEntry entry = additionalItems[index];
                if (entry == null ||
                    !ItemCatalog.IsValidItemId(interactionPointType, entry.ItemId) ||
                    entry.Quantity <= 0)
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

        private int TestProfileCount() => settings_ != null ? settings_.TestProfileCount : 1;

        private string GetProfileName(int profileIndex, int totalProfileCount)
        {
            string baseProfileName = string.IsNullOrWhiteSpace(ProfileName())
                ? "Dev Kid"
                : ProfileName().Trim();
            return totalProfileCount <= 1 ? baseProfileName : $"{baseProfileName} {profileIndex + 1}";
        }

        private string GetPetName(int profileIndex, int totalProfileCount)
        {
            string basePetName = string.IsNullOrWhiteSpace(PetName())
                ? "Brushy"
                : PetName().Trim();
            return totalProfileCount <= 1 ? basePetName : $"{basePetName} {profileIndex + 1}";
        }
    }
}
