using System.Collections.Generic;

using Game.Core.Data;

using UnityEngine;

namespace Game.Core.Configuration
{
    /// <summary>
    /// Default values used when creating a new player profile.
    /// </summary>
    public static class DefaultProfileState
    {
        public const int NoPaintItemId = 0;
        public const int InitialCoins = 0;
        public const int DefaultPetEyesItemId = 1;
        public const int DefaultPetSkinItemId = 1;
        public const int DefaultPetHatItemId = 1;
        public const int DefaultPetDressItemId = 1;

        public const bool InitialPendingReward = true;

        public static readonly Color DefaultRoomSurfaceColor = Color.white;

        public static Inventory CreateInventory()
        {
            Inventory inventory = new Inventory();

            foreach (KeyValuePair<InteractionPointType, ItemCategoryConfig> entry in ItemCatalog.All)
            {
                Dictionary<int, int> items = inventory.GetInventoryItems(entry.Key);
                ItemCategoryConfig itemConfig = entry.Value;

                for (int itemId = 1; itemId <= itemConfig.DefaultUnlockedCount; itemId++)
                {
                    items.Add(itemId, -1);
                }
            }

            return inventory;
        }
    }
}
