using System;
using System.Collections;
using System.Threading.Tasks;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.UI;

using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Scenes;

using UnityEngine;

using Zenject;

namespace Game.Unity.RewardScene
{
    /// <summary>
    /// Scene controller that grants rewards and delegates visual presentation to the reward view.
    /// </summary>
    public sealed class RewardsSceneController : SceneBase
    {
        [SerializeField]
        private RewardView rewardView_;

        private IRewardClaimService rewardClaimService_;
        private ScreenBlocker screenBlocker_;
        private IObjectInstantiator instantiator_;
        private RewardSceneFlowController flowController_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            IRewardClaimService rewardClaimService,
            ScreenBlocker screenBlocker,
            [Inject(Id = InstantiatorIds.Dependency)] IObjectInstantiator instantiator)
        {
            base.Construct(dispatcher, navigationService);
            rewardClaimService_ = rewardClaimService;
            screenBlocker_ = screenBlocker;
            instantiator_ = instantiator;
        }

        private void Awake()
        {
            sceneType_ = SceneType.RewardsScene;
            if (rewardView_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardsSceneController)} requires a {nameof(RewardView)} reference.");
            }

            rewardView_?.Configure(instantiator_);
            rewardView_?.Validate();

            flowController_ = new RewardSceneFlowController(
                rewardClaimService_,
                screenBlocker_,
                NavigationService,
                rewardView_);
        }

        protected override void Initialize()
        {
            flowController_.Initialize();
        }

        public void GiveRewards()
        {
            StartCoroutine(flowController_.GiveRewards());
        }

        public void ActiveFinalAnimationObjects()
        {
            flowController_.ActivateFinalAnimationObjects();
        }
    }

    /// <summary>
    /// Coordinates reward granting flow and delegates presentation to the reward view.
    /// </summary>
    internal sealed class RewardSceneFlowController
    {
        private readonly IRewardClaimService rewardClaimService_;
        private readonly ScreenBlocker screenBlocker_;
        private readonly IGameNavigationService navigationService_;
        private readonly RewardView rewardView_;

        private bool rewardGiven_;
        private bool waitingToFinish_;

        public RewardSceneFlowController(
            IRewardClaimService rewardClaimService,
            ScreenBlocker screenBlocker,
            IGameNavigationService navigationService,
            RewardView rewardView)
        {
            rewardClaimService_ = rewardClaimService;
            screenBlocker_ = screenBlocker;
            navigationService_ = navigationService;
            rewardView_ = rewardView ?? throw new ArgumentNullException(nameof(rewardView));
        }

        public void Initialize()
        {
            rewardGiven_ = false;
            waitingToFinish_ = false;
        }

        public void ActivateFinalAnimationObjects()
        {
            rewardView_.ActivateFinalAnimationObjects();
        }

        public IEnumerator GiveRewards()
        {
            if (waitingToFinish_)
            {
                navigationService_?.Back();
                yield break;
            }

            if (rewardGiven_ || rewardClaimService_ == null)
            {
                yield break;
            }

            Task<Reward[]> claimTask = rewardClaimService_.ClaimAsync();
            yield return new WaitUntil(() => claimTask.IsCompleted);

            if (claimTask.IsFaulted)
            {
                if (claimTask.Exception != null)
                {
                    throw claimTask.Exception;
                }

                throw new InvalidOperationException(
                    $"{nameof(RewardSceneFlowController)} failed to claim rewards.");
            }

            Reward[] rewards = claimTask.Result;
            if (rewards == null || rewards.Length < 2)
            {
                throw new InvalidOperationException(
                    $"{nameof(RewardSceneFlowController)} expected two rewards from {nameof(IRewardClaimService)}.");
            }

            rewardGiven_ = true;
            rewardView_.ShowRewards(rewards);

            IDisposable blockerScope = screenBlocker_?.BlockScope("RewardsScene");
            try
            {
                yield return rewardView_.PlayRewardAnimation(() => waitingToFinish_ = true);
            }
            finally
            {
                blockerScope?.Dispose();
            }
        }
    }
}
