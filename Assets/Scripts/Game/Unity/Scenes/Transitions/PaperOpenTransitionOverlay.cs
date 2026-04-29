using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Flowbit.Utilities.Navigation;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Reveals the new content from left to right while masking away
    /// a screenshot of the previous content. A paper edge graphic follows
    /// the reveal line to sell the transition.
    /// </summary>
    public class PaperOpenTransitionOverlay : GameSceneTransitionBase
    {
        [SerializeField]
        private GameObject visualsContainer_;

        [SerializeField]
        private RectTransform oldSceneMaskContainer_;

        [SerializeField]
        private RawImage oldSceneImage_;

        [SerializeField]
        private RectTransform paperEdge_;

        [SerializeField]
        private Image paperEdgeImage_;

        [SerializeField]
        private Sprite[] paperEdgeSprites_;

        [SerializeField]
        private float transitionDuration_ = 0.6f;

        [SerializeField]
        private bool useUnscaledTime_ = true;

        [SerializeField]
        private float edgeOffset_ = 0f;

        private Texture2D screenshotTexture_;
        private Vector2 oldSceneMaskOffsetMinInitial_;
        private Vector2 oldSceneMaskOffsetMaxInitial_;
        private Vector2 paperEdgeInitialAnchoredPosition_;
        private bool initialStateCached_;
        private int activeTransitionId_;
        private bool hasPreparedTransition_;

        private void Awake()
        {
            CacheInitialState();
            HideOverlayImmediately();
        }

        /// <summary>
        /// Captures the currently visible screen and prepares the overlay for the transition.
        /// Any transition already in progress is interrupted.
        /// </summary>
        public override IEnumerator PrepareTransition(NavigationTransitionContext _)
        {
            int transitionId = BeginNewTransition();

            yield return new WaitForEndOfFrame();

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            ReplaceTexture(ref screenshotTexture_, CaptureScreenTexture());

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            SetupOldSceneImage();
            ResetMask();
            ResetPaperEdge();

            visualsContainer_.SetActive(true);
            oldSceneMaskContainer_.gameObject.SetActive(true);
            oldSceneImage_.gameObject.SetActive(true);
            paperEdge_.gameObject.SetActive(false);

            hasPreparedTransition_ = true;
        }

        /// <summary>
        /// Animates the previous screenshot away from left to right, revealing the new content underneath.
        /// </summary>
        public override IEnumerator FinishTransition(NavigationTransitionContext _)
        {
            int transitionId = activeTransitionId_;

            if (!hasPreparedTransition_ || !IsActiveTransition(transitionId))
            {
                yield break;
            }

            yield return AnimateReveal(transitionId);

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            CompleteTransition(transitionId);
        }

        /// <summary>
        /// Starts a new transition and clears any previous visual state immediately.
        /// </summary>
        private int BeginNewTransition()
        {
            activeTransitionId_++;
            hasPreparedTransition_ = false;
            AbortCurrentVisualState();
            return activeTransitionId_;
        }

        /// <summary>
        /// Completes the transition if it is still the active one.
        /// </summary>
        private void CompleteTransition(int transitionId)
        {
            if (!IsActiveTransition(transitionId))
            {
                return;
            }

            hasPreparedTransition_ = false;
            HideOverlayImmediately();
            DestroyScreenshotTexture();
        }

        /// <summary>
        /// Returns whether the provided transition id is still active.
        /// </summary>
        private bool IsActiveTransition(int transitionId)
        {
            return transitionId == activeTransitionId_;
        }

        /// <summary>
        /// Captures the current framebuffer into a texture.
        /// </summary>
        private static Texture2D CaptureScreenTexture()
        {
            Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Replaces a texture reference and destroys the old texture if present.
        /// </summary>
        private static void ReplaceTexture(ref Texture2D currentTexture, Texture2D newTexture)
        {
            if (currentTexture != null)
            {
                Destroy(currentTexture);
            }

            currentTexture = newTexture;
        }

        /// <summary>
        /// Caches the initial transform state used by the transition.
        /// </summary>
        private void CacheInitialState()
        {
            if (initialStateCached_)
            {
                return;
            }

            if (oldSceneMaskContainer_ != null)
            {
                oldSceneMaskOffsetMinInitial_ = oldSceneMaskContainer_.offsetMin;
                oldSceneMaskOffsetMaxInitial_ = oldSceneMaskContainer_.offsetMax;
            }

            if (paperEdge_ != null)
            {
                paperEdgeInitialAnchoredPosition_ = paperEdge_.anchoredPosition;
            }

            initialStateCached_ = true;
        }

        /// <summary>
        /// Configures the captured screenshot image so it keeps its full size
        /// and is clipped by the mask instead of being stretched with it.
        /// </summary>
        private void SetupOldSceneImage()
        {
            if (oldSceneImage_ == null || oldSceneMaskContainer_ == null)
            {
                return;
            }

            RectTransform parentRect = oldSceneMaskContainer_.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            oldSceneImage_.texture = screenshotTexture_;
            oldSceneImage_.uvRect = new Rect(0f, 0f, 1f, 1f);
            oldSceneImage_.color = Color.white;

            RectTransform rectTransform = oldSceneImage_.rectTransform;

            float width = parentRect.rect.width;
            float height = parentRect.rect.height;

            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(1f, 0.5f);

            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// Restores the mask to fully show the previous screenshot.
        /// </summary>
        private void ResetMask()
        {
            if (oldSceneMaskContainer_ == null)
            {
                return;
            }

            if (!initialStateCached_)
            {
                CacheInitialState();
            }

            oldSceneMaskContainer_.offsetMin = oldSceneMaskOffsetMinInitial_;
            oldSceneMaskContainer_.offsetMax = oldSceneMaskOffsetMaxInitial_;
        }

        /// <summary>
        /// Restores the paper edge to its initial anchored position and sprite.
        /// </summary>
        private void ResetPaperEdge()
        {
            if (paperEdge_ != null)
            {
                if (!initialStateCached_)
                {
                    CacheInitialState();
                }

                paperEdge_.anchoredPosition = paperEdgeInitialAnchoredPosition_;
                paperEdge_.localRotation = Quaternion.identity;
                paperEdge_.localScale = Vector3.one;
            }

            ApplyPaperEdgeSpriteByProgress(0f);
        }

        /// <summary>
        /// Animates the reveal by shrinking the visible old-screen mask from left to right.
        /// </summary>
        private IEnumerator AnimateReveal(int transitionId)
        {
            if (oldSceneMaskContainer_ == null)
            {
                yield break;
            }

            RectTransform parentRect = oldSceneMaskContainer_.parent as RectTransform;
            if (parentRect == null)
            {
                yield break;
            }

            float parentWidth = parentRect.rect.width;
            float elapsed = 0f;

            while (elapsed < transitionDuration_)
            {
                if (!IsActiveTransition(transitionId))
                {
                    yield break;
                }

                float deltaTime = useUnscaledTime_ ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += deltaTime;

                float t = transitionDuration_ > 0f ? Mathf.Clamp01(elapsed / transitionDuration_) : 1f;
                float easedT = EaseInOutCubic(t);

                float revealedWidth = Mathf.Lerp(0f, parentWidth + 20, easedT);
                ApplyReveal(revealedWidth, parentWidth);
                ApplyPaperEdgeSpriteByProgress(t);

                yield return null;
            }

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            ApplyReveal(parentWidth, parentWidth);
            ApplyPaperEdgeSpriteByProgress(1f);
        }

        /// <summary>
        /// Applies the reveal amount and moves the paper edge to the reveal line.
        /// </summary>
        private void ApplyReveal(float revealedWidth, float parentWidth)
        {
            Vector2 offsetMin = oldSceneMaskOffsetMinInitial_;
            offsetMin.x = oldSceneMaskOffsetMinInitial_.x + revealedWidth;
            oldSceneMaskContainer_.offsetMin = offsetMin;
            oldSceneMaskContainer_.offsetMax = oldSceneMaskOffsetMaxInitial_;

            if (paperEdge_ != null)
            {
                float leftEdgeX = (-parentWidth * 0.5f) + revealedWidth;
                Vector2 anchoredPosition = paperEdgeInitialAnchoredPosition_;
                anchoredPosition.x = leftEdgeX + edgeOffset_;
                paperEdge_.anchoredPosition = anchoredPosition;
            }
        }

        /// <summary>
        /// Updates the paper edge sprite based on normalized animation progress.
        /// All sprites are distributed uniformly across the full animation duration.
        /// </summary>
        private void ApplyPaperEdgeSpriteByProgress(float normalizedProgress)
        {
            if (paperEdgeImage_ == null || paperEdgeSprites_ == null || paperEdgeSprites_.Length == 0)
            {
                return;
            }
            paperEdge_.gameObject.SetActive(true);

            int spriteCount = paperEdgeSprites_.Length;
            int spriteIndex = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01(normalizedProgress) * spriteCount), spriteCount - 1);
            paperEdgeImage_.sprite = paperEdgeSprites_[spriteIndex];
        }

        /// <summary>
        /// Interrupts and clears any current visual state immediately.
        /// </summary>
        private void AbortCurrentVisualState()
        {
            HideOverlayImmediately();
            DestroyScreenshotTexture();
            ResetMask();
            ResetPaperEdge();
        }

        /// <summary>
        /// Hides the overlay and clears the currently assigned screenshot.
        /// </summary>
        private void HideOverlayImmediately()
        {
            if (oldSceneImage_ != null)
            {
                oldSceneImage_.texture = null;
                oldSceneImage_.gameObject.SetActive(false);
            }

            if (oldSceneMaskContainer_ != null)
            {
                oldSceneMaskContainer_.gameObject.SetActive(false);
            }

            if (paperEdge_ != null)
            {
                paperEdge_.gameObject.SetActive(false);
            }

            if (visualsContainer_ != null)
            {
                visualsContainer_.SetActive(false);
            }
        }

        /// <summary>
        /// Destroys the stored screenshot texture if present.
        /// </summary>
        private void DestroyScreenshotTexture()
        {
            if (screenshotTexture_ != null)
            {
                Destroy(screenshotTexture_);
                screenshotTexture_ = null;
            }
        }

        /// <summary>
        /// Applies a cubic ease-in-out interpolation.
        /// </summary>
        private static float EaseInOutCubic(float t)
        {
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }

            float value = (-2f * t) + 2f;
            return 1f - ((value * value * value) * 0.5f);
        }
    }
}