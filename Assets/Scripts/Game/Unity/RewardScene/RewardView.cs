using System;
using System.Collections;
using System.Collections.Generic;

using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.UI;

using Game.Core.Data;
using Game.Unity.RoomScene;

using UnityEngine;

namespace Game.Unity.RewardScene
{
    /// <summary>
    /// Encapsulates the visual presentation and animation for the rewards scene.
    /// </summary>
    public sealed class RewardView : MonoBehaviour
    {
        [SerializeField]
        private RoomInventoryListElement rewardElementPrefab_;

        [SerializeField]
        private RectTransform rewardContainer1_;

        [SerializeField]
        private RectTransform rewardContainer2_;

        [SerializeField]
        private UIComponentAnimatorController rewardAnimation_;

        [SerializeField]
        private GameObject finalAnimationObject_;

        [SerializeField]
        private float animationDuration_ = 3f;

        private readonly List<RoomInventoryListElement> spawnedRewardViews_ = new();
        private IObjectInstantiator instantiator_;

        public void Configure(IObjectInstantiator instantiator)
        {
            instantiator_ = instantiator;
        }

        public void Validate()
        {
            if (rewardElementPrefab_ == null ||
                rewardContainer1_ == null ||
                rewardContainer2_ == null ||
                rewardAnimation_ == null ||
                finalAnimationObject_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardView)} is missing one or more required references.");
            }
        }

        public void ShowRewards(Reward[] rewards)
        {
            if (rewards == null || rewards.Length < 2)
            {
                throw new ArgumentException(
                    $"{nameof(RewardView)} expected two rewards to present.",
                    nameof(rewards));
            }

            ShowReward(rewards[0], rewardContainer1_);
            ShowReward(rewards[1], rewardContainer2_);
        }

        public void ActivateFinalAnimationObjects()
        {
            if (finalAnimationObject_ != null)
            {
                finalAnimationObject_.SetActive(true);
            }
        }

        public IEnumerator PlayRewardAnimation(Action onCompleted)
        {
            rewardAnimation_.GoToFinalState();
            yield return new WaitForSeconds(animationDuration_);
            onCompleted?.Invoke();
        }

        private void ShowReward(Reward reward, RectTransform container)
        {
            if (container == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardView)} requires a valid reward container.");
            }

            RoomInventoryListElement rewardView = instantiator_ != null
                ? instantiator_.InstantiatePrefab(rewardElementPrefab_, container)
                : Instantiate(rewardElementPrefab_, container, false);
            spawnedRewardViews_.Add(rewardView);

            rewardView.Show(CreateRewardItemData(reward));
            rewardView.transform.localPosition = Vector3.zero;
            rewardView.transform.localScale = Vector3.one;
            rewardView.transform.localRotation = Quaternion.identity;

            RectTransform rewardRect = rewardView.transform as RectTransform;
            DisableRewardDragging(rewardView);
        }

        private RoomInventoryItemData CreateRewardItemData(Reward reward)
        {
            if (reward == null)
            {
                throw new ArgumentNullException(nameof(reward));
            }

            return new RoomInventoryItemData
            {
                ItemId = reward.Id,
                InteractionPointType = reward.RewardType,
                Quantity = reward.Quantity,
                Color = ResolveRewardColor(reward)
            };
        }

        private static Color ResolveRewardColor(Reward reward)
        {
            if (reward.RewardType == InteractionPointType.PAINT ||
                reward.RewardType == InteractionPointType.SKIN)
            {
                return RoomItemVisuals.GetItemColor(reward.RewardType, reward.Id);
            }

            return Color.white;
        }

        private static void DisableRewardDragging(RoomInventoryListElement rewardView)
        {
            RoomInventoryDraggable draggable = rewardView.GetComponent<RoomInventoryDraggable>();
            if (draggable != null)
            {
                draggable.enabled = false;
            }
        }
    }
}
