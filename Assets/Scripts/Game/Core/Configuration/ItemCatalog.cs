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
                { InteractionPointType.DRESS, new ItemCategoryConfig(20, 1, 2) },
                { InteractionPointType.EYES, new ItemCategoryConfig(4, 1, 2) },
                { InteractionPointType.FOOD, new ItemCategoryConfig(16, 0, 2) },
                { InteractionPointType.HAT, new ItemCategoryConfig(20, 1, 2) },
                { InteractionPointType.PAINT, new ItemCategoryConfig(31, 3, 2) },
                { InteractionPointType.PLACEABLE_OBJECT, new ItemCategoryConfig(107, 0, 1) },
                { InteractionPointType.SKIN, new ItemCategoryConfig(26, 1, 2) },
            };

        public static IReadOnlyDictionary<InteractionPointType, ItemCategoryConfig> All => all_;

        public static ItemCategoryConfig Get(InteractionPointType interactionPointType) => all_[interactionPointType];
    }

    /// <summary>
    /// Configuration for one item category in the catalog.
    /// </summary>
    public sealed class ItemCategoryConfig
    {
        public ItemCategoryConfig(int itemCount, int defaultUnlockedCount, int rewardQuantity)
        {
            ItemCount = itemCount;
            DefaultUnlockedCount = defaultUnlockedCount;
            RewardQuantity = rewardQuantity;
        }

        public int ItemCount { get; }

        public int DefaultUnlockedCount { get; }

        public int RewardQuantity { get; }
    }
}
