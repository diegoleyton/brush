using System;
using System.Collections.Generic;

using Game.Core.Data;

namespace Game.Core.Configuration
{
    /// <summary>
    /// Generated market catalog for purchasable items and their prices.
    /// </summary>
    public static class MarketCatalog
    {
        private static readonly IReadOnlyDictionary<(InteractionPointType, int), MarketItemDefinition> all_ =
            BuildCatalog();

        /// <summary>
        /// Gets the full generated market catalog.
        /// </summary>
        public static IReadOnlyDictionary<(InteractionPointType, int), MarketItemDefinition> All => all_;

        /// <summary>
        /// Tries to find a market listing for the given item.
        /// </summary>
        public static bool TryGet(InteractionPointType itemType, int itemId, out MarketItemDefinition definition) =>
            all_.TryGetValue((itemType, itemId), out definition);

        private static IReadOnlyDictionary<(InteractionPointType, int), MarketItemDefinition> BuildCatalog()
        {
            Dictionary<(InteractionPointType, int), MarketItemDefinition> catalog =
                new Dictionary<(InteractionPointType, int), MarketItemDefinition>();

            foreach (KeyValuePair<InteractionPointType, ItemCategoryConfig> entry in ItemCatalog.All)
            {
                InteractionPointType itemType = entry.Key;
                ItemCategoryConfig category = entry.Value;

                for (int itemId = category.DefaultUnlockedCount + 1; itemId <= category.ItemCount; itemId++)
                {
                    catalog[(itemType, itemId)] = new MarketItemDefinition
                    {
                        ItemType = itemType,
                        ItemId = itemId,
                        CurrencyType = CurrencyType.Coins,
                        Price = GetGeneratedPrice(itemType, itemId),
                        Quantity = GetGeneratedQuantity(itemType)
                    };
                }
            }

            return catalog;
        }

        private static int GetGeneratedPrice(InteractionPointType itemType, int itemId)
        {
            int tier = Math.Max(0, (itemId - 1) / 10);

            switch (itemType)
            {
                case InteractionPointType.DRESS:
                    return 24 + (tier * 6);
                case InteractionPointType.FOOD:
                    return 5 + tier;
                case InteractionPointType.HAT:
                    return 22 + (tier * 5);
                case InteractionPointType.PAINT:
                    return 12 + (tier * 3);
                case InteractionPointType.FACE:
                    return 20 + (tier * 5);
                case InteractionPointType.SKIN:
                    return 25 + (tier * 6);
                case InteractionPointType.PLACEABLE_OBJECT:
                    return 30 + (tier * 8);
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }
        }

        private static int GetGeneratedQuantity(InteractionPointType itemType)
        {
            switch (itemType)
            {
                case InteractionPointType.FOOD:
                    return 3;
                case InteractionPointType.HAT:
                case InteractionPointType.DRESS:
                case InteractionPointType.PAINT:
                    return 2;
                default:
                    return 1;
            }
        }
    }
}
