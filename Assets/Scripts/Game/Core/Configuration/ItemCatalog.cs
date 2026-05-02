using System.Collections.Generic;

using Game.Core.Data;

namespace Game.Core.Configuration
{
    /// <summary>
    /// Static item catalog grouped by interaction type.
    /// </summary>
    public static class ItemCatalog
    {
        private static readonly IReadOnlyDictionary<InteractionPointType, ItemCategoryConfig> all_ =
            new Dictionary<InteractionPointType, ItemCategoryConfig>
            {
                { InteractionPointType.DRESS, new ItemCategoryConfig(CreateRange(0, 19), 1, 2) },
                { InteractionPointType.EYES, new ItemCategoryConfig(CreateRange(1, 4), 1, 2) },
                { InteractionPointType.FOOD, new ItemCategoryConfig(CreateRange(1, 16), 0, 2) },
                { InteractionPointType.HAT, new ItemCategoryConfig(CreateRange(1, 20), 1, 2) },
                { InteractionPointType.PAINT, new ItemCategoryConfig(CreateRange(1, 31), 3, 2) },
                { InteractionPointType.PLACEABLE_OBJECT, new ItemCategoryConfig(CreateRange(1, 16), 0, 1) },
                { InteractionPointType.SKIN, new ItemCategoryConfig(CreateRange(1, 26), 1, 2) },
            };

        public static IReadOnlyDictionary<InteractionPointType, ItemCategoryConfig> All => all_;

        public static ItemCategoryConfig Get(InteractionPointType interactionPointType) => all_[interactionPointType];

        public static bool IsValidItemId(InteractionPointType interactionPointType, int itemId)
        {
            return Get(interactionPointType).IsValidItemId(itemId);
        }

        private static int[] CreateRange(int minInclusive, int maxInclusive)
        {
            int[] values = new int[(maxInclusive - minInclusive) + 1];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = minInclusive + index;
            }

            return values;
        }
    }

    /// <summary>
    /// Configuration for one item category in the catalog.
    /// </summary>
    public sealed class ItemCategoryConfig
    {
        public ItemCategoryConfig(int[] validItemIds, int defaultUnlockedCount, int rewardQuantity)
        {
            ValidItemIds = validItemIds ?? new int[0];
            DefaultUnlockedCount = defaultUnlockedCount;
            RewardQuantity = rewardQuantity;
        }

        public int ItemCount => ValidItemIds.Count > 0 ? ValidItemIds[ValidItemIds.Count - 1] : 0;

        public IReadOnlyList<int> ValidItemIds { get; }

        public int DefaultUnlockedCount { get; }

        public int RewardQuantity { get; }

        public bool IsValidItemId(int itemId)
        {
            for (int index = 0; index < ValidItemIds.Count; index++)
            {
                if (ValidItemIds[index] == itemId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
