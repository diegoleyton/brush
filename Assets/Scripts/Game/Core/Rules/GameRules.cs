using UnityEngine;

using Game.Core.Configuration;
using Game.Core.Data;

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

        private static readonly InteractionPointType[] possibleRandomRewardTypes_ =
        {
            InteractionPointType.DRESS,
            InteractionPointType.EYES,
            InteractionPointType.HAT,
            InteractionPointType.SKIN,
            InteractionPointType.PAINT,
            InteractionPointType.PLACEABLE_OBJECT
        };

        public static Reward GetRandomReward()
        {
            InteractionPointType randomRewardType =
                possibleRandomRewardTypes_[Random.Range(0, possibleRandomRewardTypes_.Length)];
            return GetRandomReward(randomRewardType);
        }

        public static Reward GetRandomReward(InteractionPointType rewardType)
        {
            ItemCategoryConfig itemConfig = ItemCatalog.Get(rewardType);
            return new Reward
            {
                Id = Random.Range(itemConfig.DefaultUnlockedCount + 1, itemConfig.ItemCount + 1),
                RewardType = rewardType,
                Quantity = itemConfig.RewardQuantity
            };
        }
    }
}
