namespace Marmilo.Domain.GameState;

public static class InGameMarketCatalog
{
    private const int InteractionPointTypePlaceableObject = 0;
    private const int InteractionPointTypePaint = 1;
    private const int InteractionPointTypeSkin = 2;
    private const int InteractionPointTypeHat = 3;
    private const int InteractionPointTypeDress = 4;
    private const int InteractionPointTypeFood = 5;
    private const int InteractionPointTypeEyes = 6;

    private static readonly IReadOnlyDictionary<int, CategoryConfig> categories_ =
        new Dictionary<int, CategoryConfig>
        {
            { InteractionPointTypeDress, new CategoryConfig(CreateRange(0, 19), defaultUnlockedCount: 1, rewardQuantity: 2) },
            { InteractionPointTypeEyes, new CategoryConfig(CreateRange(1, 4), defaultUnlockedCount: 1, rewardQuantity: 2) },
            { InteractionPointTypeFood, new CategoryConfig(CreateRange(1, 16), defaultUnlockedCount: 0, rewardQuantity: 2) },
            { InteractionPointTypeHat, new CategoryConfig(CreateRange(1, 20), defaultUnlockedCount: 1, rewardQuantity: 2) },
            { InteractionPointTypePaint, new CategoryConfig(CreateRange(1, 31), defaultUnlockedCount: 3, rewardQuantity: 2) },
            { InteractionPointTypePlaceableObject, new CategoryConfig(CreateRange(1, 16), defaultUnlockedCount: 0, rewardQuantity: 1) },
            { InteractionPointTypeSkin, new CategoryConfig(CreateRange(1, 26), defaultUnlockedCount: 1, rewardQuantity: 2) },
        };

    public static bool TryGet(int itemType, int itemId, out InGameMarketItemDefinition definition)
    {
        definition = null!;

        if (!categories_.TryGetValue(itemType, out CategoryConfig? category) ||
            !category.ValidItemIds.Contains(itemId) ||
            Array.IndexOf(category.ValidItemIds, itemId) < category.DefaultUnlockedCount)
        {
            return false;
        }

        definition = new InGameMarketItemDefinition(
            itemType,
            itemId,
            GetGeneratedPrice(itemType, itemId),
            GetGeneratedQuantity(itemType));

        return true;
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

    private static int GetGeneratedPrice(int itemType, int itemId)
    {
        int tier = Math.Max(0, (itemId - 1) / 10);

        return itemType switch
        {
            InteractionPointTypeDress => 24 + (tier * 6),
            InteractionPointTypeFood => 5 + tier,
            InteractionPointTypeHat => 22 + (tier * 5),
            InteractionPointTypePaint => 12 + (tier * 3),
            InteractionPointTypeEyes => 20 + (tier * 5),
            InteractionPointTypeSkin => 25 + (tier * 6),
            InteractionPointTypePlaceableObject => 30 + (tier * 8),
            _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
        };
    }

    private static int GetGeneratedQuantity(int itemType)
    {
        return itemType switch
        {
            InteractionPointTypeFood => 3,
            InteractionPointTypeHat => 2,
            InteractionPointTypeDress => 2,
            InteractionPointTypePaint => 2,
            _ => 1
        };
    }

    public sealed record InGameMarketItemDefinition(
        int ItemType,
        int ItemId,
        int Price,
        int Quantity);

    private sealed class CategoryConfig
    {
        public CategoryConfig(int[] validItemIds, int defaultUnlockedCount, int rewardQuantity)
        {
            ValidItemIds = validItemIds;
            DefaultUnlockedCount = defaultUnlockedCount;
            RewardQuantity = rewardQuantity;
        }

        public int[] ValidItemIds { get; }

        public int DefaultUnlockedCount { get; }

        public int RewardQuantity { get; }
    }
}
