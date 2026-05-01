using UnityEngine;

using Game.Core.Configuration;
using Game.Core.Data;
using System.Collections.Generic;

namespace Game.Core.Rules
{
    /// <summary>
    /// Core gameplay rules used by the repository and other domain services.
    /// </summary>
    public static class GameRules
    {
        public const int CurrencyRewardAmount = 5;
        public const float EatBlockedAfterBrushSeconds = 2 * 60 * 60;
        public const float EatLimitWindowSeconds = 2 * 60 * 60;
        public const float BrushCooldownSeconds = 5 * 60 * 60;

        private static bool forcePlaceableObjectRewardsOnly_;

        private static readonly WeightedRewardDefinition[] possibleRandomRewardTypes_ =
        {
            new WeightedRewardDefinition(RewardKind.Currency, weight: 70),
            new WeightedRewardDefinition(RewardKind.Item, weight: 10, rewardType: InteractionPointType.EYES),
            new WeightedRewardDefinition(RewardKind.Item, weight: 10, rewardType: InteractionPointType.PLACEABLE_OBJECT),
            new WeightedRewardDefinition(RewardKind.Item, weight: 10, rewardType: InteractionPointType.SKIN)
        };

        private static readonly IReadOnlyDictionary<InteractionPointType, int[]> rewardItemIdWhitelist_ =
            new Dictionary<InteractionPointType, int[]>
            {
                { InteractionPointType.FOOD, CreateRange(1, 16) },
                { InteractionPointType.EYES, CreateRange(1, 4) },
                { InteractionPointType.PLACEABLE_OBJECT, CreateRange(1, 16) }
            };

        public static Reward GetGuaranteedFoodReward()
        {
            if (forcePlaceableObjectRewardsOnly_)
            {
                return CreateItemReward(
                    InteractionPointType.FOOD,
                    ResolveRandomRewardItemId(InteractionPointType.FOOD, ItemCatalog.Get(InteractionPointType.FOOD)),
                    Random.Range(2, 5));
            }

            ItemCategoryConfig foodConfig = ItemCatalog.Get(InteractionPointType.FOOD);
            return CreateItemReward(
                InteractionPointType.FOOD,
                ResolveRandomRewardItemId(InteractionPointType.FOOD, foodConfig),
                Random.Range(2, 5));
        }

        public static Reward GetRandomReward()
        {
            if (forcePlaceableObjectRewardsOnly_)
            {
                return GetRandomReward(InteractionPointType.PLACEABLE_OBJECT);
            }

            int totalWeight = 0;
            for (int index = 0; index < possibleRandomRewardTypes_.Length; index++)
            {
                totalWeight += possibleRandomRewardTypes_[index].Weight;
            }

            int roll = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            for (int index = 0; index < possibleRandomRewardTypes_.Length; index++)
            {
                WeightedRewardDefinition weightedDefinition = possibleRandomRewardTypes_[index];
                cumulativeWeight += weightedDefinition.Weight;
                if (roll >= cumulativeWeight)
                {
                    continue;
                }

                if (weightedDefinition.Kind == RewardKind.Currency)
                {
                    return CreateCurrencyReward(CurrencyType.Coins, CurrencyRewardAmount);
                }

                return GetRandomReward(weightedDefinition.RewardType);
            }

            return CreateCurrencyReward(CurrencyType.Coins, CurrencyRewardAmount);
        }

        public static Reward GetRandomReward(InteractionPointType rewardType)
        {
            ItemCategoryConfig itemConfig = ItemCatalog.Get(rewardType);
            return CreateItemReward(
                rewardType,
                ResolveRandomRewardItemId(rewardType, itemConfig),
                itemConfig.RewardQuantity);
        }

        private static int ResolveRandomRewardItemId(
            InteractionPointType rewardType,
            ItemCategoryConfig itemConfig)
        {
            if (rewardItemIdWhitelist_.TryGetValue(rewardType, out int[] whitelistedIds) &&
                whitelistedIds != null &&
                whitelistedIds.Length > 0)
            {
                return whitelistedIds[Random.Range(0, whitelistedIds.Length)];
            }

            return Random.Range(itemConfig.DefaultUnlockedCount + 1, itemConfig.ItemCount + 1);
        }

        private static int[] CreateRange(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                return new int[0];
            }

            int[] values = new int[(maxInclusive - minInclusive) + 1];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = minInclusive + index;
            }

            return values;
        }

        public static void SetForcePlaceableObjectRewardsOnly(bool enabled)
        {
            forcePlaceableObjectRewardsOnly_ = enabled;
        }

        private static Reward CreateItemReward(InteractionPointType rewardType, int itemId, int quantity)
        {
            return new Reward
            {
                Kind = RewardKind.Item,
                RewardType = rewardType,
                Id = itemId,
                Quantity = quantity
            };
        }

        private static Reward CreateCurrencyReward(CurrencyType currencyType, int amount)
        {
            return new Reward
            {
                Kind = RewardKind.Currency,
                CurrencyType = currencyType,
                Quantity = amount
            };
        }

        private readonly struct WeightedRewardDefinition
        {
            public WeightedRewardDefinition(
                RewardKind kind,
                int weight,
                InteractionPointType rewardType = InteractionPointType.FOOD)
            {
                Kind = kind;
                Weight = weight;
                RewardType = rewardType;
            }

            public RewardKind Kind { get; }
            public int Weight { get; }
            public InteractionPointType RewardType { get; }
        }
    }
}
