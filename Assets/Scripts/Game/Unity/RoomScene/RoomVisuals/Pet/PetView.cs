using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Game.Unity.Definitions;
using Game.Core.Data;
using Flowbit.Utilities.Unity.AssetLoader;
using UnityEngine.UI;
using Zenject;
using System;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for a pet.
    /// </summary>
    public sealed class PetView : MonoBehaviour
    {
        public event System.Action EatAnimationCompleted;
        public event System.Action PaintAnimationCompleted;

        private class TriggerNames
        {
            public string BodyTriggerName { get; private set; }
            public string MouthTriggerName { get; private set; }

            public TriggerNames(string bodyTriggerName, string mouthTriggerName)
            {
                MouthTriggerName = mouthTriggerName;
                BodyTriggerName = bodyTriggerName;
            }
        }

        private const string HappyName = "Happy";
        private const string PrepareToEatName = "PreEat";

        private const string IdleName = "Idle";

        private const string FunName = "Fun";

        private const string mouthEatName = "Eat";

        private const string mouthPostEatName = "PostEat";

        private const string mouthCleanName = "Clean";

        private const string mouthNoMoreFoodName = "NoMoreFood";

        private const string mouthIdleName = "Idle";

        private const string mouthPreEatName = "PreEat";

        private const string mouthHappyName = "Happy";

        private const string mouthFunName = "Fun";

        private const string mouthPaintName = "Sourprise";

        private readonly static Dictionary<PetEatStatus, TriggerNames> preEatTriggerNames_ = new Dictionary<PetEatStatus, TriggerNames>
        {
            {PetEatStatus.OK, new TriggerNames(PrepareToEatName, mouthPreEatName)},
            {PetEatStatus.NO_AFTER_BRUSHING, new TriggerNames(IdleName, mouthCleanName)},
            {PetEatStatus.NO_MORE, new TriggerNames(IdleName, mouthNoMoreFoodName)}
        };

        private static readonly TriggerNames happyTriggerNames_ = new TriggerNames(HappyName, mouthHappyName);
        private static readonly TriggerNames iddleTriggerNames_ = new TriggerNames(IdleName, mouthIdleName);

        private static readonly TriggerNames eatTriggerNames_ = new TriggerNames(IdleName, mouthEatName);
        private static readonly TriggerNames postEatTriggerNames_ = new TriggerNames(IdleName, mouthPostEatName);

        private static readonly TriggerNames funTriggerNames_ = new TriggerNames(FunName, mouthFunName);
        private static readonly TriggerNames paintTriggerNames_ = new TriggerNames(IdleName, mouthPaintName);

        [SerializeField]
        private Animator bodyAnimationController_;

        [SerializeField]
        private Animator mouthAnimationController_;

        [SerializeField]
        private float eatDuration_ = 3f;

        [SerializeField]
        private float postEatDuration_ = 2f;

        [SerializeField]
        private float paintDuration_ = 2f;

        [SerializeField]
        private float postPaintDuration_ = 1f;

        [SerializeField]
        private RectTransform foodVisualPrefab_;

        [SerializeField]
        private Transform mouthTarget_;

        [SerializeField]
        private Vector3 foodFinalScale_ = Vector3.one * 0.4f;

        [SerializeField]
        private float foodTravelDuration_ = 0.5f;

        [SerializeField]
        private float foodFadeDuration_ = 0.2f;

        private IAssetLoader assetLoader_;
        private IAssetLoadHandle<Sprite> pendingFoodSpriteHandle_;
        private RectTransform activeFoodVisual_;

        private bool isHappy_ = false;

        private bool isPreparingToEat_ = false;

        private bool isEating_ = false;

        private bool isPainting_ = false;

        private PetEatStatus petEatState_;

        [Inject]
        public void Construct(IAssetLoader assetLoader)
        {
            assetLoader_ = assetLoader;
        }

        private void Awake()
        {
            if (bodyAnimationController_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetView)} requires a body animator reference.");
            }

            if (mouthAnimationController_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetView)} requires a mouth animator reference.");
            }

            if (foodVisualPrefab_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetView)} requires a food visual prefab reference.");
            }

            if (mouthTarget_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetView)} requires a mouth target reference.");
            }
        }

        private void OnDestroy()
        {
            if (activeFoodVisual_ != null)
            {
                Destroy(activeFoodVisual_.gameObject);
                activeFoodVisual_ = null;
            }

            ReleaseFoodHandle();
        }

        /// <summary>
        /// Plays the pet eat presentation.
        /// </summary>
        public void Eat()
        {
            Eat(itemId: 0, dropScreenPosition: null);
        }

        public void Eat(int itemId, Vector2? dropScreenPosition)
        {
            if (isEating_ || isPainting_)
            {
                return;
            }

            isEating_ = true;
            StartCoroutine(EatCoroutine(itemId, dropScreenPosition));
        }

        /// <summary>
        /// Prepares the pet to receive food.
        /// </summary>
        public void PrepareToEat(PetEatStatus peteatState)
        {
            isPreparingToEat_ = true;
            petEatState_ = peteatState;
            if (isEating_ || isPainting_)
            {
                return;
            }

            SetTrigger(preEatTriggerNames_[petEatState_]);
        }

        public void ExitFoodState()
        {
            isEating_ = false;
            isPreparingToEat_ = false;
            GoToNextState();
        }

        /// <summary>
        /// Plays the pet paint presentation.
        /// </summary>
        public void Paint()
        {
            if (isPainting_ || isEating_)
            {
                return;
            }

            isPainting_ = true;
            StartCoroutine(PaintCoroutine());
            SetTrigger(paintTriggerNames_);
        }

        /// <summary>
        /// Exits the pet paint presentation state.
        /// </summary>
        public void ExitPaintState()
        {
            isPainting_ = false;
            GoToNextState();
        }

        /// <summary>
        /// Plays the pet single-touch reaction.
        /// </summary>
        public void Touch()
        {
            if (isPreparingToEat_ || isEating_ || isPainting_)
            {
                return;
            }

            SetTrigger(funTriggerNames_);
        }

        /// <summary>
        /// Starts the pet continuous touching reaction.
        /// </summary>
        public void StartTouching()
        {
            isHappy_ = true;

            if (isPreparingToEat_ || isEating_ || isPainting_)
            {
                return;
            }

            SetTrigger(happyTriggerNames_);
        }

        /// <summary>
        /// Stops the pet continuous touching reaction.
        /// </summary>
        public void StopTouching()
        {
            isHappy_ = false;
            GoToNextState();
        }

        private void GoToNextState()
        {
            if (isEating_ || isPainting_)
            {
                return;
            }
            if (isPreparingToEat_)
            {
                SetTrigger(preEatTriggerNames_[petEatState_]);
                return;
            }
            if (isHappy_)
            {
                SetTrigger(happyTriggerNames_);
                return;
            }

            SetTrigger(iddleTriggerNames_);
        }

        private void SetTrigger(TriggerNames triggerNames)
        {
            if (triggerNames.BodyTriggerName != null)
            {
                bodyAnimationController_.SetTrigger(triggerNames.BodyTriggerName);
            }
            if (triggerNames.MouthTriggerName != null)
            {
                mouthAnimationController_.SetTrigger(triggerNames.MouthTriggerName);
            }
        }

        private IEnumerator EatCoroutine(int itemId, Vector2? dropScreenPosition)
        {
            if (itemId > 0)
            {
                yield return StartCoroutine(PlayFoodVisualAnimation(itemId, dropScreenPosition));
            }
            else
            {
                SetTrigger(eatTriggerNames_);
            }

            yield return new WaitForSeconds(eatDuration_);
            SetTrigger(postEatTriggerNames_);
            yield return new WaitForSeconds(postEatDuration_);
            ExitFoodState();
            EatAnimationCompleted?.Invoke();
        }

        private IEnumerator PaintCoroutine()
        {
            SetTrigger(paintTriggerNames_);
            yield return new WaitForSeconds(paintDuration_);
            SetTrigger(funTriggerNames_);
            yield return new WaitForSeconds(postPaintDuration_);
            ExitPaintState();
            PaintAnimationCompleted?.Invoke();
        }

        private IEnumerator PlayFoodVisualAnimation(int itemId, Vector2? dropScreenPosition)
        {
            if (assetLoader_ == null)
            {
                yield break;
            }

            string assetName = AssetNameResolver.GetFoodAssetName(itemId);
            bool completed = false;
            Sprite loadedSprite = null;

            ReleaseFoodHandle();
            pendingFoodSpriteHandle_ = assetLoader_.LoadAssetAsync<Sprite>(assetName, sprite =>
            {
                loadedSprite = sprite;
                completed = true;
            });

            while (!completed)
            {
                yield return null;
            }

            if (loadedSprite == null)
            {
                ReleaseFoodHandle();
                yield break;
            }

            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            RectTransform rootCanvasTransform = rootCanvas != null
                ? rootCanvas.transform as RectTransform
                : null;
            RectTransform mouthRectTransform = mouthTarget_ as RectTransform;

            if (rootCanvasTransform == null || mouthRectTransform == null)
            {
                ReleaseFoodHandle();
                yield break;
            }

            if (activeFoodVisual_ != null)
            {
                Destroy(activeFoodVisual_.gameObject);
                activeFoodVisual_ = null;
            }

            activeFoodVisual_ = Instantiate(foodVisualPrefab_, rootCanvasTransform, false);
            activeFoodVisual_.SetAsLastSibling();

            Image foodImage = activeFoodVisual_.GetComponentInChildren<Image>(true);
            if (foodImage == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PetView)} food visual prefab requires an Image.");
            }

            foodImage.sprite = loadedSprite;
            foodImage.color = Color.white;

            Vector2 startPosition = ResolveFoodStartAnchoredPosition(rootCanvasTransform, rootCanvas, dropScreenPosition);
            Vector2 targetPosition = ResolveRectTransformAnchoredPosition(rootCanvasTransform, rootCanvas, mouthRectTransform);
            Vector3 startScale = activeFoodVisual_.localScale;

            float travelDuration = Mathf.Max(0f, foodTravelDuration_);
            if (travelDuration <= 0f)
            {
                activeFoodVisual_.anchoredPosition = targetPosition;
                activeFoodVisual_.localScale = foodFinalScale_;
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < travelDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / travelDuration);
                    activeFoodVisual_.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                    activeFoodVisual_.localScale = Vector3.Lerp(startScale, foodFinalScale_, t);
                    yield return null;
                }
            }

            activeFoodVisual_.anchoredPosition = targetPosition;
            activeFoodVisual_.localScale = foodFinalScale_;

            float fadeDuration = Mathf.Max(0f, foodFadeDuration_);
            SetTrigger(eatTriggerNames_);

            if (fadeDuration > 0f)
            {
                float elapsed = 0f;
                Color startColor = foodImage.color;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);
                    Color color = startColor;
                    color.a = Mathf.Lerp(1f, 0f, t);
                    foodImage.color = color;
                    yield return null;
                }
            }

            Destroy(activeFoodVisual_.gameObject);
            activeFoodVisual_ = null;
            ReleaseFoodHandle();
        }

        private Vector2 ResolveFoodStartAnchoredPosition(
            RectTransform rootCanvasTransform,
            Canvas rootCanvas,
            Vector2? dropScreenPosition)
        {
            if (!dropScreenPosition.HasValue)
            {
                return ResolveRectTransformAnchoredPosition(rootCanvasTransform, rootCanvas, rectTransform: transform as RectTransform);
            }

            Camera eventCamera = rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootCanvasTransform,
                    dropScreenPosition.Value,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return localPoint;
            }

            return ResolveRectTransformAnchoredPosition(rootCanvasTransform, rootCanvas, rectTransform: transform as RectTransform);
        }

        private static Vector2 ResolveRectTransformAnchoredPosition(
            RectTransform rootCanvasTransform,
            Canvas rootCanvas,
            RectTransform rectTransform)
        {
            if (rootCanvasTransform == null || rootCanvas == null || rectTransform == null)
            {
                return Vector2.zero;
            }

            Camera eventCamera = rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, rectTransform.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootCanvasTransform,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return localPoint;
            }

            return Vector2.zero;
        }

        private void ReleaseFoodHandle()
        {
            if (pendingFoodSpriteHandle_ == null)
            {
                return;
            }

            pendingFoodSpriteHandle_.Release();
            pendingFoodSpriteHandle_ = null;
        }
    }
}
