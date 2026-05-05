using System;
using System.Collections;
using System.Threading.Tasks;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.RemoteCommunication;
using Flowbit.Utilities.ScreenBlocker;
using Flowbit.Utilities.Unity.Instantiator;

using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Scenes;
using Game.Unity.UI;

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
                StartRoutine,
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

        private void StartRoutine(IEnumerator routine)
        {
            if (routine != null)
            {
                StartCoroutine(routine);
            }
        }
    }

    /// <summary>
    /// Coordinates reward granting flow and delegates presentation to the reward view.
    /// </summary>
    internal sealed class RewardSceneFlowController
    {
        private readonly Action<IEnumerator> startCoroutine_;
        private readonly IRewardClaimService rewardClaimService_;
        private readonly ScreenBlocker screenBlocker_;
        private readonly IGameNavigationService navigationService_;
        private readonly RewardView rewardView_;

        private bool rewardGiven_;
        private bool waitingToFinish_;

        public RewardSceneFlowController(
            Action<IEnumerator> startCoroutine,
            IRewardClaimService rewardClaimService,
            ScreenBlocker screenBlocker,
            IGameNavigationService navigationService,
            RewardView rewardView)
        {
            startCoroutine_ = startCoroutine ?? throw new ArgumentNullException(nameof(startCoroutine));
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

            IDisposable claimBlockerScope = screenBlocker_?.BlockScope(
                "RewardsSceneClaim",
                showLoadingWithTime: true,
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.rewards.claim",
                    "Claiming reward..."));
            Task<Reward[]> claimTask = rewardClaimService_.ClaimAsync();
            yield return new WaitUntil(() => claimTask.IsCompleted);
            claimBlockerScope?.Dispose();

            if (claimTask.IsFaulted)
            {
                HandleClaimFailure(UnwrapTaskException(claimTask.Exception));
                yield break;
            }

            Reward[] rewards = claimTask.Result;
            if (rewards == null || rewards.Length < 2)
            {
                ShowClaimErrorPopup(
                    LocalizationServiceLocator.GetText(
                        "rewards.claim.error",
                        "Could not claim rewards."));
                yield break;
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

        private void HandleClaimFailure(Exception exception)
        {
            string fallbackMessage = LocalizationServiceLocator.GetText(
                "rewards.claim.error",
                "Could not claim rewards.");
            string message = RemoteOperationMessageFormatter.Format(exception, fallbackMessage);

            if (exception is RemoteRequestFailedException remoteException && remoteException.IsNetworkError)
            {
                navigationService_?.Navigate(
                    SceneType.RetryPopup,
                    new RetryPopupParams(
                        message,
                        onRetry: () => startCoroutine_(GiveRewards()),
                        onCancel: navigationService_.Back,
                        title: LocalizationServiceLocator.GetText(
                            "rewards.retry.popup_title",
                            "Connection problem")));
                return;
            }

            ShowClaimErrorPopup(message);
        }

        private void ShowClaimErrorPopup(string message)
        {
            navigationService_.Navigate(
                SceneType.ErrorPopup,
                new ErrorPopupParams(
                    message,
                    title: LocalizationServiceLocator.GetText(
                        "rewards.error.popup_title",
                        "Could not claim reward"),
                    onClose: navigationService_.Back));
        }

        private static Exception UnwrapTaskException(AggregateException exception)
        {
            if (exception == null)
            {
                return new InvalidOperationException("Reward claim failed.");
            }

            return exception.InnerExceptions.Count == 1 && exception.InnerException != null
                ? exception.InnerException
                : exception.Flatten();
        }
    }
}
