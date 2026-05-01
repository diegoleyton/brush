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
        public const float EatBlockedAfterBrushSeconds = 2 * 60 * 60;
        public const float EatLimitWindowSeconds = 2 * 60 * 60;
        public const float BrushCooldownSeconds = 5 * 60 * 60;

        private static bool forcePlaceableObjectRewardsOnly_;

        private static readonly InteractionPointType[] possibleRandomRewardTypes_ =
        {
            InteractionPointType.DRESS,
            InteractionPointType.EYES,
            InteractionPointType.HAT,
            InteractionPointType.SKIN,
            InteractionPointType.PAINT,
            InteractionPointType.PLACEABLE_OBJECT
        };

        private static readonly IReadOnlyDictionary<InteractionPointType, int[]> rewardItemIdWhitelist_ =
            new Dictionary<InteractionPointType, int[]>
            {
                { InteractionPointType.FOOD, CreateRange(1, 16) },
                { InteractionPointType.EYES, CreateRange(1, 4) },
                { InteractionPointType.PLACEABLE_OBJECT, CreateRange(1, 16) }
            };

        public static Reward GetRandomReward()
        {
            if (forcePlaceableObjectRewardsOnly_)
            {
                return GetRandomReward(InteractionPointType.PLACEABLE_OBJECT);
            }

            InteractionPointType randomRewardType =
                possibleRandomRewardTypes_[Random.Range(0, possibleRandomRewardTypes_.Length)];
            return GetRandomReward(randomRewardType);
        }

        public static Reward GetRandomReward(InteractionPointType rewardType)
        {
            if (forcePlaceableObjectRewardsOnly_)
            {
                rewardType = InteractionPointType.PLACEABLE_OBJECT;
            }

            ItemCategoryConfig itemConfig = ItemCatalog.Get(rewardType);
            int rewardItemId = ResolveRandomRewardItemId(rewardType, itemConfig);
            return new Reward
            {
                Id = rewardItemId,
                RewardType = rewardType,
                Quantity = itemConfig.RewardQuantity
            };
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
    }
}
