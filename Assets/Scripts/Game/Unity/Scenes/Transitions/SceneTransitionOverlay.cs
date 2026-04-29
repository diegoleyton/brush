using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Flowbit.Utilities.Navigation;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Displays a screenshot of the previous scene on top of everything
    /// while the next scene loads in the background.
    /// </summary>
    public class SceneTransitionOverlay : GameSceneTransitionBase
    {
        [SerializeField]
        private RawImage rawImage_;

        [SerializeField]
        private GameObject visualsContainer_;

        [SerializeField]
        private RectTransform finalImage_;

        [SerializeField]
        private MaskableContainer[] maskableContainers_;

        [SerializeField]
        private float transitionOutDuration_ = 1f;

        [SerializeField]
        private float throwDuration_ = 0.6f;

        [SerializeField]
        private float throwHeight_ = 40f;

        [SerializeField]
        private float throwRotationDegrees_ = 180f;

        [SerializeField]
        private RectTransform throwTarget_;

        private Texture2D screenshotTexture_;
        private Vector2 finalImageInitialAnchoredPosition_;
        private Quaternion finalImageInitialLocalRotation_;
        private bool finalImageInitialStateCached_;
        private Vector3 backgroundPosition_;

        private int activeTransitionId_;
        private bool hasPreparedTransition_;

        private void Awake()
        {
            CacheInitialFinalImageState();
            visualsContainer_.gameObject.SetActive(false);
            DisableAllMaskableContainers();
        }

        /// <summary>
        /// Prepares a new transition and interrupts any previous one that may still be running.
        /// </summary>
        public override IEnumerator PrepareTransition(NavigationTransitionContext _)
        {
            int transitionId = BeginNewTransition();

            yield return new WaitForEndOfFrame();

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            if (screenshotTexture_ != null)
            {
                Destroy(screenshotTexture_);
                screenshotTexture_ = null;
            }

            screenshotTexture_ = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshotTexture_.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshotTexture_.Apply();

            rawImage_.texture = screenshotTexture_;
            rawImage_.color = Color.white;
            rawImage_.rectTransform.SetParent(visualsContainer_.transform, false);

            CacheInitialFinalImageState();
            ResetFinalImageTransform();

            visualsContainer_.gameObject.SetActive(true);
            DisableAllMaskableContainers();

            hasPreparedTransition_ = true;

            yield return null;

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            yield return null;
        }

        /// <summary>
        /// Finishes the currently active transition if it has not been interrupted.
        /// </summary>
        public override IEnumerator FinishTransition(NavigationTransitionContext _)
        {
            int transitionId = activeTransitionId_;

            if (!hasPreparedTransition_ || !IsActiveTransition(transitionId))
            {
                yield break;
            }

            DisableAllMaskableContainers();

            if (maskableContainers_ == null || maskableContainers_.Length == 0)
            {
                CompleteTransition(transitionId);
                yield break;
            }

            float waitPerContainer = transitionOutDuration_ / (maskableContainers_.Length + 1);
            MaskableContainer previousContainer = null;

            for (int i = 0; i < maskableContainers_.Length; i++)
            {
                if (!IsActiveTransition(transitionId))
                {
                    yield break;
                }

                MaskableContainer currentContainer = maskableContainers_[i];

                if (previousContainer != null)
                {
                    previousContainer.Enable(false);
                }

                currentContainer.Enable(true);
                currentContainer.AddBackground(rawImage_.rectTransform);
                rawImage_.transform.position = backgroundPosition_;

                previousContainer = currentContainer;

                yield return new WaitForSeconds(waitPerContainer);
            }

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            if (previousContainer != null)
            {
                previousContainer.Enable(false);
            }

            ResetFinalImageTransform();
            finalImage_?.gameObject.SetActive(true);

            yield return new WaitForSeconds(waitPerContainer);

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            yield return StartCoroutine(ThrowToTarget(finalImage_, transitionId));

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            DisableAllMaskableContainers();
            rawImage_.rectTransform.SetParent(visualsContainer_.transform, true);
            rawImage_.transform.position = backgroundPosition_;

            CompleteTransition(transitionId);
        }

        /// <summary>
        /// Starts a new transition and aborts any previous visual state.
        /// </summary>
        private int BeginNewTransition()
        {
            activeTransitionId_++;
            hasPreparedTransition_ = false;
            AbortVisualState();
            return activeTransitionId_;
        }

        /// <summary>
        /// Completes the given transition if it is still the active one.
        /// </summary>
        private void CompleteTransition(int transitionId)
        {
            if (!IsActiveTransition(transitionId))
            {
                return;
            }

            hasPreparedTransition_ = false;
            Clean();
        }

        /// <summary>
        /// Returns whether the given transition id is still the active one.
        /// </summary>
        private bool IsActiveTransition(int transitionId)
        {
            return transitionId == activeTransitionId_;
        }

        /// <summary>
        /// Resets the visual state immediately to interrupt any running transition.
        /// </summary>
        private void AbortVisualState()
        {
            DisableAllMaskableContainers();
            ResetFinalImageTransform();

            if (rawImage_ != null)
            {
                rawImage_.texture = null;
                rawImage_.rectTransform.SetParent(visualsContainer_.transform, false);
                rawImage_.transform.position = backgroundPosition_;
            }

            if (screenshotTexture_ != null)
            {
                Destroy(screenshotTexture_);
                screenshotTexture_ = null;
            }

            if (visualsContainer_ != null)
            {
                visualsContainer_.gameObject.SetActive(false);
            }
        }

        private void DisableAllMaskableContainers()
        {
            if (maskableContainers_ != null)
            {
                for (int i = 0; i < maskableContainers_.Length; i++)
                {
                    if (maskableContainers_[i] != null)
                    {
                        maskableContainers_[i].Enable(false);
                    }
                }
            }

            if (finalImage_ != null)
            {
                finalImage_.gameObject.SetActive(false);
            }
        }

        private void Clean()
        {
            ResetFinalImageTransform();

            rawImage_.texture = null;

            if (screenshotTexture_ != null)
            {
                Destroy(screenshotTexture_);
                screenshotTexture_ = null;
            }

            visualsContainer_.gameObject.SetActive(false);
        }

        /// <summary>
        /// Throws a UI element from its current position to the target using a projectile-like arc.
        /// </summary>
        private IEnumerator ThrowToTarget(RectTransform element, int transitionId)
        {
            if (element == null || throwTarget_ == null)
            {
                yield break;
            }

            RectTransform parentRect = element.parent as RectTransform;
            if (parentRect == null)
            {
                yield break;
            }

            Vector2 startPosition = element.anchoredPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                RectTransformUtility.WorldToScreenPoint(null, throwTarget_.position),
                null,
                out Vector2 endPosition);

            float elapsed = 0f;

            while (elapsed < throwDuration_)
            {
                if (!IsActiveTransition(transitionId))
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / throwDuration_);

                Vector2 position = Vector2.Lerp(startPosition, endPosition, t);

                float arcOffset = 4f * t * (1f - t) * throwHeight_;
                position.y += arcOffset;

                element.anchoredPosition = position;
                element.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(0f, throwRotationDegrees_, t));

                yield return null;
            }

            if (!IsActiveTransition(transitionId))
            {
                yield break;
            }

            element.anchoredPosition = endPosition;
            element.localRotation = Quaternion.Euler(0f, 0f, throwRotationDegrees_);

            ResetFinalImageTransform();
        }

        /// <summary>
        /// Caches the initial transform state of the final image so it can be reused on subsequent transitions.
        /// </summary>
        private void CacheInitialFinalImageState()
        {
            if (finalImage_ == null || finalImageInitialStateCached_)
            {
                return;
            }

            finalImageInitialAnchoredPosition_ = finalImage_.anchoredPosition;
            finalImageInitialLocalRotation_ = finalImage_.localRotation;
            finalImageInitialStateCached_ = true;

            backgroundPosition_ = rawImage_.transform.position;
        }

        /// <summary>
        /// Restores the final image to its initial anchored position and rotation.
        /// </summary>
        private void ResetFinalImageTransform()
        {
            if (finalImage_ == null)
            {
                return;
            }

            if (!finalImageInitialStateCached_)
            {
                CacheInitialFinalImageState();
            }

            finalImage_.anchoredPosition = finalImageInitialAnchoredPosition_;
            finalImage_.localRotation = finalImageInitialLocalRotation_;
        }
    }

    [Serializable]
    public class MaskableContainer
    {
        [SerializeField]
        private GameObject mainConatiner_;

        [SerializeField]
        private RectTransform maskConatiner_;

        /// <summary>
        /// Enables or disables the full visual container for this mask step.
        /// </summary>
        public void Enable(bool enabled)
        {
            if (mainConatiner_ != null)
            {
                mainConatiner_.SetActive(enabled);
            }
        }

        /// <summary>
        /// Parents the given background under this mask container.
        /// </summary>
        public void AddBackground(RectTransform background)
        {
            if (background != null && maskConatiner_ != null)
            {
                background.SetParent(maskConatiner_, true);
            }
        }
    }
}