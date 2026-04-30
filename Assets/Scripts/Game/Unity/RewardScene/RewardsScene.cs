using System;
using System.Collections;
using System.Collections.Generic;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.UI;

using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.RoomScene;
using Game.Unity.Scenes;

using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.RewardScene
{
    /// <summary>
    /// Presents the rewards granted after a completed gameplay flow and returns when the user confirms.
    /// </summary>
    public sealed class RewardsScene : SceneBase
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
        private float animationDuration_ = 3f;

        private readonly List<RoomInventoryListElement> spawnedRewardViews_ = new();

        private DataRepository dataRepository_;
        private ScreenBlocker screenBlocker_;
        private IObjectInstantiator instantiator_;
        private bool rewardGiven_;
        private bool waitingToFinish_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            DataRepository dataRepository,
            ScreenBlocker screenBlocker,
            [Inject(Id = InstantiatorIds.Dependency)] IObjectInstantiator instantiator)
        {
            base.Construct(dispatcher, navigationService);
            dataRepository_ = dataRepository;
            screenBlocker_ = screenBlocker;
            instantiator_ = instantiator;
        }

        private void Awake()
        {
            sceneType_ = SceneType.RewardsScene;
            ValidateSerializedReferences();
        }

        private void OnDestroy()
        {
            ClearSpawnedRewards();
        }

        protected override void Initialize()
        {
            rewardGiven_ = false;
            waitingToFinish_ = false;
            ClearSpawnedRewards();
        }

        public void GiveRewards()
        {
            if (waitingToFinish_)
            {
                NavigationService.Back();
                return;
            }

            if (rewardGiven_ || dataRepository_ == null)
            {
                return;
            }

            Reward[] rewards = dataRepository_.GiveRewards();
            if (rewards == null || rewards.Length < 2)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardsScene)} expected two rewards from {nameof(DataRepository.GiveRewards)}.");
            }

            rewardGiven_ = true;
            ClearSpawnedRewards();

            ShowReward(rewards[0], rewardContainer1_);
            ShowReward(rewards[1], rewardContainer2_);

            StartCoroutine(ShowRewardAnimation());
        }

        private void ShowReward(Reward reward, RectTransform container)
        {
            if (container == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardsScene)} requires a valid reward container.");
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
            if (rewardRect != null)
            {
                rewardRect.anchorMin = new Vector2(0.5f, 0.5f);
                rewardRect.anchorMax = new Vector2(0.5f, 0.5f);
                rewardRect.pivot = new Vector2(0.5f, 0.5f);
                rewardRect.anchoredPosition = Vector2.zero;
                rewardRect.sizeDelta = container.rect.size;
            }

            DisableRewardDragging(rewardView);
            SetRewardBackgroundAlpha(rewardView, 0f);
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

        private IEnumerator ShowRewardAnimation()
        {
            IDisposable blockerScope = screenBlocker_?.BlockScope("RewardsScene");
            try
            {
                yield return new WaitForSeconds(0.5f);
                rewardAnimation_.GoToFinalState();
                yield return new WaitForSeconds(animationDuration_);
                waitingToFinish_ = true;
            }
            finally
            {
                blockerScope?.Dispose();
            }
        }

        private void ClearSpawnedRewards()
        {
            for (int index = 0; index < spawnedRewardViews_.Count; index++)
            {
                RoomInventoryListElement rewardView = spawnedRewardViews_[index];
                if (rewardView == null)
                {
                    continue;
                }

                Destroy(rewardView.gameObject);
            }

            spawnedRewardViews_.Clear();
        }

        private static void DisableRewardDragging(RoomInventoryListElement rewardView)
        {
            RoomInventoryDraggable draggable = rewardView.GetComponent<RoomInventoryDraggable>();
            if (draggable != null)
            {
                draggable.enabled = false;
            }
        }

        private static void SetRewardBackgroundAlpha(RoomInventoryListElement rewardView, float alpha)
        {
            Image background = rewardView.GetComponent<Image>();
            if (background == null)
            {
                return;
            }

            Color color = background.color;
            color.a = alpha;
            background.color = color;
        }

        private static CanvasGroup EnsureCanvasGroup(RectTransform container)
        {
            if (container == null)
            {
                return null;
            }

            CanvasGroup canvasGroup = container.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = container.gameObject.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        private void ValidateSerializedReferences()
        {
            if (rewardElementPrefab_ == null ||
                rewardContainer1_ == null ||
                rewardContainer2_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardsScene)} is missing one or more required serialized references.");
            }
        }
    }
}
