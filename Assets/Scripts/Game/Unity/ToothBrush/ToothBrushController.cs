using System;
using System.Collections;
using System.Collections.Generic;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.ScreenBlocker;
using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.RoomScene;
using Game.Unity.Scenes;

using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.ToothBrush
{
    /// <summary>
    /// Defines the visual orientation of the toothbrush during the brushing sequence.
    /// </summary>
    public enum BrushOrientation
    {
        Front,
        Bottom,
        Back
    }

    /// <summary>
    /// Identifies the mouth quadrant currently being brushed.
    /// </summary>
    public enum MouthPosition
    {
        TopRight,
        TopLeft,
        BottomRight,
        BottomLeft
    }

    /// <summary>
    /// Describes the source of a pause request in the toothbrush scene.
    /// </summary>
    public enum PauseType
    {
        PAUSE_MENU,
        INTERNAL,
    }

    /// <summary>
    /// Controls the toothbrush minigame flow, visuals, pause state, and reward completion.
    /// </summary>
    public class ToothbrushingScene : SceneBase
    {
        private const float DefaultMoveDuration = 0.3f;

        private DataRepository dataRepository_;
        private IRoomGameplayService roomGameplayService_;
        private ScreenBlocker screenBlocker_;
        private EventDispatcher dispatcher_;

        [Header("Mouth Parts")]
        [SerializeField] private RectTransform mouthTop;
        [SerializeField] private RectTransform mouthBottom;
        [SerializeField] private RectTransform mouthBackground;
        [SerializeField] private RectTransform panel1;
        [SerializeField] private RectTransform panel2;
        [SerializeField] private RectTransform panel3;

        [Header("Brush")]
        [SerializeField] private RectTransform brushTransform;
        [SerializeField] private Image brushImage;
        [SerializeField] private Image brushImage2;
        [SerializeField] private Sprite brushFront;
        [SerializeField] private Sprite brushBottom;
        [SerializeField] private Sprite brushBottom2;
        [SerializeField] private Sprite brushBack;
        [SerializeField] private Sprite brushBack2;
        [SerializeField] private LoopMovement brushAnim_;

        [SerializeField] private Image[] brushImages_;
        [SerializeField] private BrushTimer[] brushTimers_;

        [Header("Animation Settings")]
        private float moveDuration = DefaultMoveDuration;
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private float initialWaitDuration = 4f;
        [SerializeField] private float zoneWaitDuration = 2f;
        [SerializeField] private float finishDuration = 5f;

        [Header("Positions")]
        [SerializeField] private List<MouthTarget> targets_;
        [SerializeField] private RectTransform leftTransitionTransform_;
        [SerializeField] private RectTransform rightTransitionTransform_;

        [Header("Background")]
        [SerializeField] private Image petImage_;

        private RectTransform brushImageTransform;
        private float brushImageStartY;
        private float brushImageStartYRot;
        private bool isInTop_;
        private readonly PauseService pauseService_ = new PauseService();
        private MouthTarget currentZone_;
        private bool started_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            DataRepository dataRepository,
            IRoomGameplayService roomGameplayService,
            ScreenBlocker screenBlocker)
        {
            base.Construct(dispatcher, navigationService);
            dataRepository_ = dataRepository;
            roomGameplayService_ = roomGameplayService;
            screenBlocker_ = screenBlocker;
            dispatcher_ = dispatcher;
        }

        private void Awake()
        {
            sceneType_ = SceneType.BrushScene;
            ValidateSerializedReferences();
        }

        public void Pause()
        {
            NavigationService.Navigate(SceneType.ConfirmPopup, new ConfirmPopupParams(Play, NavigationService.Back));
            PauseInt(PauseType.PAUSE_MENU);
        }

        public void Play()
        {
            Continue(PauseType.PAUSE_MENU);

            if (started_)
            {
                return;
            }

            started_ = true;
            brushAnim_.Pause((int)PauseType.INTERNAL);
            StartCoroutine(BrushAll());
            brushImageTransform = brushImage.rectTransform;
            brushImageStartY = brushImageTransform.anchoredPosition.y;
            brushImageStartYRot = brushImageTransform.localEulerAngles.z;
        }

        protected override void Initialize()
        {
            brushAnim_.Pause();
            started_ = false;
            moveDuration = ResolveMoveDuration();

            if (dataRepository_?.CurrentProfile?.PetData != null)
            {
                int skinItemId = dataRepository_.CurrentProfile.PetData.SkinItemId;
                Color skinColor = RoomItemVisuals.GetItemColor(InteractionPointType.SKIN, skinItemId);
                SetPetColor(skinColor);
            }

            int brushPaintItemId = roomGameplayService_ != null
                ? roomGameplayService_.GetRoomSurfacePaintId(RoomSurfaceIds.Toothbrush)
                : DefaultProfileState.NoPaintItemId;
            Color brushColor = brushPaintItemId > DefaultProfileState.NoPaintItemId
                ? RoomItemVisuals.GetItemColor(InteractionPointType.PAINT, brushPaintItemId)
                : DefaultProfileState.DefaultRoomSurfaceColor;
            SetBrushColor(brushColor);

            Play();
        }

        private void OnFinish()
        {
            if (roomGameplayService_ != null && roomGameplayService_.Brush())
            {
                dataRepository_.SetPendingReward(true);
                NavigationService.NavigateWithoutHistory(SceneType.RewardsScene);
                return;
            }
            NavigationService.Back();
        }

        private void PauseInt(PauseType pauseType)
        {
            pauseService_.Pause((int)pauseType);
            brushAnim_.Pause((int)pauseType);
            currentZone_.dirt?.Pause((int)pauseType);
            currentZone_.timer?.Pause((int)pauseType);
            dispatcher_?.Send(new BrushPausedEvent(true));
        }

        private void Continue(PauseType pauseType)
        {
            pauseService_.Play((int)pauseType);
            brushAnim_.Play((int)pauseType);
            currentZone_.dirt?.Play((int)pauseType);
            currentZone_.timer?.Play((int)pauseType);
            dispatcher_?.Send(new BrushPausedEvent(false));
        }

        private void SetPetColor(Color color)
        {
            if (petImage_ == null)
            {
                return;
            }

            petImage_.color = color;
        }

        private void SetBrushColor(Color color)
        {
            if (brushImages_ == null)
            {
                return;
            }

            for (int index = 0; index < brushImages_.Length; index++)
            {
                Image brushGraphic = brushImages_[index];
                if (brushGraphic == null)
                {
                    continue;
                }

                brushGraphic.color = color;
            }

            for (int index = 0; index < brushTimers_.Length; index++)
            {
                BrushTimer brushTimers = brushTimers_[index];
                if (brushTimers == null)
                {
                    continue;
                }

                brushTimers.SetColor(color);
            }
        }

        private IEnumerator BrushAll()
        {
            for (int i = 0; i < targets_.Count; i++)
            {
                MouthTarget target = targets_[i];
                currentZone_ = target;
                isInTop_ = target.position == MouthPosition.TopLeft || target.position == MouthPosition.TopRight;

                RectTransform transitionTransform =
                    target.position == MouthPosition.BottomRight || target.position == MouthPosition.TopRight
                        ? rightTransitionTransform_
                        : leftTransitionTransform_;

                brushAnim_.Pause((int)PauseType.INTERNAL);
                yield return StartCoroutine(MoveAndBrushCoroutine(transitionTransform, transitionDuration));
                SetBrushOrientation(BrushOrientation.Back, target.position);
                yield return StartCoroutine(Wait(initialWaitDuration));

                target.timer?.StartTimer(GetTargetRemainingDurationAfterInitialWait());

                target.dirt?.Run(moveDuration);
                brushAnim_.Play((int)PauseType.INTERNAL);
                dispatcher_?.Send(new InteractionEvent(InteractionEventType.BRUSH_STARTED));
                yield return Move(target.rectTransform1, target.rectTransform2);
                brushAnim_.Pause((int)PauseType.INTERNAL);
                dispatcher_?.Send(new InteractionEvent(InteractionEventType.BRUSH_ENDED));

                yield return StartCoroutine(MoveAndBrushCoroutine(transitionTransform, transitionDuration));
                SetBrushOrientation(BrushOrientation.Bottom, target.position);
                yield return StartCoroutine(Wait(zoneWaitDuration));
                target.dirt?.Run(moveDuration);
                brushAnim_.Play((int)PauseType.INTERNAL);
                dispatcher_?.Send(new InteractionEvent(InteractionEventType.BRUSH_STARTED));
                yield return Move(target.rectTransform2Side, target.rectTransform1Side);
                brushAnim_.Pause((int)PauseType.INTERNAL);
                dispatcher_?.Send(new InteractionEvent(InteractionEventType.BRUSH_ENDED));

                yield return StartCoroutine(MoveAndBrushCoroutine(transitionTransform, transitionDuration));
                SetBrushOrientation(BrushOrientation.Front, target.position);
                yield return StartCoroutine(Wait(zoneWaitDuration));
                target.dirt?.Run(moveDuration);
                brushAnim_.Play((int)PauseType.INTERNAL);
                dispatcher_?.Send(new InteractionEvent(InteractionEventType.BRUSH_STARTED));
                yield return Move(target.rectTransform1, target.rectTransform2);
                brushAnim_.Pause((int)PauseType.INTERNAL);
                dispatcher_?.Send(new InteractionEvent(InteractionEventType.BRUSH_ENDED));

                dispatcher_?.Send(new InteractionEvent(InteractionEventType.BRUSH_ZONE_READY));
            }

            IDisposable blockerScope = screenBlocker_?.BlockScope("ToothbrushFinish");
            try
            {
                StartCoroutine(FadeOut(brushImage2, transitionDuration));
                yield return StartCoroutine(FadeOut(brushImage, transitionDuration));
                yield return StartCoroutine(Wait(finishDuration));
            }
            finally
            {
                blockerScope?.Dispose();
            }

            OnFinish();
        }

        private float ResolveMoveDuration()
        {
            if (targets_ == null || targets_.Count <= 0)
            {
                return DefaultMoveDuration;
            }

            float desiredMinutes = dataRepository_?.GetBrushSessionDurationMinutes() ??
                DefaultProfileState.DefaultBrushSessionDurationMinutes;
            float desiredSeconds = desiredMinutes * 60f;
            float fixedDuration =
                (targets_.Count * (initialWaitDuration + (2f * zoneWaitDuration) + (6f * transitionDuration))) +
                transitionDuration +
                finishDuration;
            float moveBudget = desiredSeconds - fixedDuration;
            float calculatedMoveDuration = moveBudget / (targets_.Count * 3f);

            return calculatedMoveDuration >= DefaultMoveDuration
                ? calculatedMoveDuration
                : DefaultMoveDuration;
        }

        private float GetTargetRemainingDurationAfterInitialWait() =>
            (3f * moveDuration) + (5f * transitionDuration) + (2f * zoneWaitDuration);

        private IEnumerator Move(RectTransform target1, RectTransform target2)
        {
            yield return StartCoroutine(MoveAndBrushCoroutine(target1, transitionDuration));
            yield return StartCoroutine(MoveAndBrushCoroutine(target2, moveDuration));
        }

        private IEnumerator Wait(float duration)
        {
            float t = 0f;

            while (t < duration)
            {
                if (pauseService_.IsPaused)
                {
                    yield return null;
                    continue;
                }

                t += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator MoveAndBrushCoroutine(RectTransform target, float duration)
        {
            Vector3 start = brushTransform.position;
            Quaternion startRot = brushTransform.rotation;
            Vector3 end = target.position;
            Quaternion endRot = Quaternion.Euler(target.rotation.eulerAngles);
            float t = 0f;

            while (t < duration)
            {
                if (pauseService_.IsPaused)
                {
                    yield return null;
                    continue;
                }

                t += Time.deltaTime;
                brushTransform.position = Vector3.Lerp(start, end, t / duration);
                brushTransform.rotation = Quaternion.Lerp(startRot, endRot, t / duration);
                yield return null;
            }

            brushTransform.position = end;
            brushTransform.rotation = endRot;
        }

        private void SetBrushOrientation(BrushOrientation orientation, MouthPosition mouthPosition)
        {
            switch (orientation)
            {
                case BrushOrientation.Front:
                    brushImage.sprite = brushFront;
                    brushImage2.gameObject.SetActive(false);
                    SetMouthContent(mouthTop, mouthBottom, brushTransform);
                    break;

                case BrushOrientation.Bottom:
                    brushImage.sprite = brushBottom;
                    brushImage2.sprite = brushBottom2;
                    brushImage2.gameObject.SetActive(true);
                    SetMouthContent(mouthTop, mouthBottom, brushTransform);
                    break;

                case BrushOrientation.Back:
                    brushImage.sprite = brushBack;
                    brushImage2.sprite = brushBack2;
                    brushImage2.gameObject.SetActive(true);
                    if (isInTop_)
                    {
                        SetMouthContent(mouthBottom, brushTransform, mouthTop);
                    }
                    else
                    {
                        SetMouthContent(mouthTop, brushTransform, mouthBottom);
                    }
                    break;
            }

            float xScale =
                orientation == BrushOrientation.Bottom &&
                (mouthPosition == MouthPosition.TopRight || mouthPosition == MouthPosition.BottomLeft)
                    ? -1f
                    : 1f;

            brushImage.transform.localScale = new Vector3(xScale, 1f, 1f);
        }

        private void SetMouthContent(RectTransform child1, RectTransform child2, RectTransform child3)
        {
            child1.SetParent(panel1);
            child2.SetParent(panel2);
            child3.SetParent(panel3);
        }

        private IEnumerator FadeOut(Image img, float duration)
        {
            Color startColor = img.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            float t = 0f;
            while (t < duration)
            {
                if (pauseService_.IsPaused)
                {
                    yield return null;
                    continue;
                }

                t += Time.deltaTime;
                img.color = Color.Lerp(startColor, endColor, t / duration);
                yield return null;
            }

            img.color = endColor;
            Destroy(img.gameObject);
        }

        private void ValidateSerializedReferences()
        {
            if (mouthTop == null ||
                mouthBottom == null ||
                panel1 == null ||
                panel2 == null ||
                panel3 == null ||
                brushTransform == null ||
                brushImage == null ||
                brushFront == null ||
                brushBottom == null ||
                brushBack == null ||
                brushAnim_ == null ||
                leftTransitionTransform_ == null ||
                rightTransitionTransform_ == null ||
                petImage_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ToothbrushingScene)} is missing one or more required serialized references.");
            }
        }
    }

    [Serializable]
    public struct MouthTarget
    {
        public MouthPosition position;
        public RectTransform rectTransform1;
        public RectTransform rectTransform2;
        public RectTransform rectTransform1Side;
        public RectTransform rectTransform2Side;
        public DirtFadeOutSequence dirt;
        public BrushTimer timer;
    }
}
