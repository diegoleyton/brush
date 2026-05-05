using System;
using System.Collections;
using System.Threading.Tasks;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.RemoteCommunication;
using Flowbit.Utilities.ScreenBlocker;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.RoomScene;
using Game.Unity.Scenes;
using Game.Unity.UI;

using UnityEngine;

using Zenject;

namespace Game.Unity.ToothBrush
{
    /// <summary>
    /// Scene controller for the toothbrush minigame. Delegates all visuals and animation playback to the view.
    /// </summary>
    public sealed class ToothBrushingSceneController : SceneBase
    {
        [SerializeField]
        private ToothBrushView view_;

        private DataRepository dataRepository_;
        private IRoomGameplayService roomGameplayService_;
        private IBrushCompletionService brushCompletionService_;
        private ScreenBlocker screenBlocker_;
        private EventDispatcher dispatcher_;
        private bool started_;
        private bool isCompletingBrushSession_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            DataRepository dataRepository,
            IRoomGameplayService roomGameplayService,
            IBrushCompletionService brushCompletionService,
            ScreenBlocker screenBlocker)
        {
            base.Construct(dispatcher, navigationService);
            dataRepository_ = dataRepository;
            roomGameplayService_ = roomGameplayService;
            brushCompletionService_ = brushCompletionService;
            screenBlocker_ = screenBlocker;
            dispatcher_ = dispatcher;
        }

        private void Awake()
        {
            sceneType_ = SceneType.BrushScene;
            ValidateSerializedReferences();
        }

        protected override void Initialize()
        {
            started_ = false;
            isCompletingBrushSession_ = false;

            int skinItemId = dataRepository_?.CurrentProfile?.PetData?.SkinItemId ??
                DefaultProfileState.DefaultPetSkinItemId;
            Color skinColor = RoomItemVisuals.GetItemColor(InteractionPointType.SKIN, skinItemId);

            int brushPaintItemId = roomGameplayService_ != null
                ? roomGameplayService_.GetRoomSurfacePaintId(RoomSurfaceIds.Toothbrush)
                : DefaultProfileState.NoPaintItemId;
            Color brushColor = brushPaintItemId > DefaultProfileState.NoPaintItemId
                ? RoomItemVisuals.GetItemColor(InteractionPointType.PAINT, brushPaintItemId)
                : DefaultProfileState.DefaultRoomSurfaceColor;

            float desiredBrushDurationMinutes = dataRepository_?.GetBrushSessionDurationMinutes() ??
                DefaultProfileState.DefaultBrushSessionDurationMinutes;

            view_.Prepare(desiredBrushDurationMinutes, skinColor, brushColor, OnSequenceFinished);
            Play();
        }

        public void Pause()
        {
            NavigationService.Navigate(
                SceneType.ConfirmPopup,
                new ConfirmPopupParams(Play, NavigationService.Back));
            view_.PauseVisuals(PauseType.PAUSE_MENU);
            dispatcher_?.Send(new BrushPausedEvent(true));
        }

        public void Play()
        {
            view_.ResumeVisuals(PauseType.PAUSE_MENU);
            dispatcher_?.Send(new BrushPausedEvent(false));

            if (started_)
            {
                return;
            }

            started_ = true;
            view_.BeginBrushSequence();
        }

        private void OnSequenceFinished()
        {
            if (!isCompletingBrushSession_)
            {
                StartCoroutine(CompleteBrushSessionAndContinue());
            }
        }

        private IEnumerator CompleteBrushSessionAndContinue()
        {
            if (brushCompletionService_ == null || isCompletingBrushSession_)
            {
                yield break;
            }

            isCompletingBrushSession_ = true;

            IDisposable blockScope = screenBlocker_?.BlockScope(new ScreenBlockerRequest
            {
                Reason = "BrushCompletion",
                ShowLoadingWithTime = true,
                LoadingMessage = LocalizationServiceLocator.GetText(
                    "loading.brush.complete",
                    "Finishing brush session..."),
                ShowLoadingImmediately = true
            });

            Task<bool> completionTask = brushCompletionService_.CompleteCurrentAsync();
            yield return new WaitUntil(() => completionTask.IsCompleted);
            blockScope?.Dispose();

            if (completionTask.IsFaulted)
            {
                isCompletingBrushSession_ = false;
                HandleBrushCompletionFailure(UnwrapTaskException(completionTask.Exception));
                yield break;
            }

            isCompletingBrushSession_ = false;

            if (!completionTask.Result)
            {
                ShowBrushCompletionErrorPopup(
                    LocalizationServiceLocator.GetText(
                        "brush.complete.error",
                        "Could not complete brush session."));
                yield break;
            }

            NavigationService.NavigateWithoutHistory(SceneType.RewardsScene);
        }

        private void HandleBrushCompletionFailure(Exception exception)
        {
            if (exception is AuthSessionInvalidatedException)
            {
                return;
            }

            string fallbackMessage = LocalizationServiceLocator.GetText(
                "brush.complete.error",
                "Could not complete brush session.");
            string message = RemoteOperationMessageFormatter.Format(exception, fallbackMessage);

            if (exception is RemoteRequestFailedException remoteException && remoteException.IsNetworkError)
            {
                NavigationService.Navigate(
                    SceneType.RetryPopup,
                    new RetryPopupParams(
                        message,
                        onRetry: () => StartCoroutine(CompleteBrushSessionAndContinue()),
                        onCancel: NavigationService.Back,
                        title: LocalizationServiceLocator.GetText(
                            "brush.retry.popup_title",
                            "Connection problem")));
                return;
            }

            ShowBrushCompletionErrorPopup(message);
        }

        private void ShowBrushCompletionErrorPopup(string message)
        {
            NavigationService.Navigate(
                SceneType.ErrorPopup,
                new ErrorPopupParams(
                    message,
                    title: LocalizationServiceLocator.GetText(
                        "brush.error.popup_title",
                        "Could not finish brushing"),
                    onClose: NavigationService.Back));
        }

        private static Exception UnwrapTaskException(AggregateException exception)
        {
            if (exception == null)
            {
                return new InvalidOperationException("Brush completion failed.");
            }

            return exception.InnerExceptions.Count == 1 && exception.InnerException != null
                ? exception.InnerException
                : exception.Flatten();
        }

        private void ValidateSerializedReferences()
        {
            if (view_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ToothBrushingSceneController)} requires a {nameof(ToothBrushView)} reference.");
            }
        }
    }
}
