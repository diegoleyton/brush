using System;
using System.Collections;
using UnityEngine;
using Flowbit.Utilities.Navigation;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Defines when a popup transition should execute.
    /// </summary>
    public enum PopupTransitionPhase
    {
        Prepare = 0,
        Finish = 1,
    }

    /// <summary>
    /// Defines a logical anchor used to resolve popup positions during transitions.
    /// </summary>
    public enum PopupTransitionAnchor
    {
        Center = 0,
        TopLeftOffscreen = 1,
        BottomRightOffscreen = 2,
    }

    /// <summary>
    /// Animates a popup between two configured states using its root RectTransform.
    /// </summary>
    public sealed class PopupTransformTransition : GameSceneTransitionBase
    {
        [SerializeField]
        private PopupTransitionPhase executionPhase_ = PopupTransitionPhase.Finish;

        [SerializeField]
        private PopupTransitionAnchor startAnchor_ = PopupTransitionAnchor.TopLeftOffscreen;

        [SerializeField]
        private PopupTransitionAnchor endAnchor_ = PopupTransitionAnchor.Center;

        [SerializeField]
        private float duration_ = 0.35f;

        [SerializeField]
        private float offscreenPaddingFactor_ = 0.75f;

        [SerializeField]
        private float startRotation_ = -18f;

        [SerializeField]
        private float endRotation_ = 0f;

        [SerializeField]
        private float startScale_ = 0.94f;

        [SerializeField]
        private float endScale_ = 1f;

        [SerializeField]
        private AnimationCurve positionCurve_ = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        private AnimationCurve rotationCurve_ = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        private AnimationCurve scaleCurve_ = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        /// <summary>
        /// Prepares a transition if this strategy is configured to execute during the prepare phase.
        /// </summary>
        public override IEnumerator PrepareTransition(NavigationTransitionContext context)
        {
            if (executionPhase_ != PopupTransitionPhase.Prepare)
            {
                yield break;
            }

            yield return RunTransition(context);
        }

        /// <summary>
        /// Finishes a transition if this strategy is configured to execute during the finish phase.
        /// </summary>
        public override IEnumerator FinishTransition(NavigationTransitionContext context)
        {
            if (executionPhase_ != PopupTransitionPhase.Finish)
            {
                yield break;
            }

            yield return RunTransition(context);
        }

        private IEnumerator RunTransition(NavigationTransitionContext context)
        {
            PopupBase popup = ResolvePopup(context);

            RectTransform root = popup.Root;
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PopupBase)} must expose a valid {nameof(PopupBase.Root)}.");
            }

            Canvas canvas = popup.Canvas;
            if (canvas == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PopupBase)} must expose a valid {nameof(PopupBase.Canvas)}.");
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                throw new InvalidOperationException(
                    $"{nameof(PopupTransformTransition)} expects the popup canvas to use {nameof(RenderMode.ScreenSpaceOverlay)}.");
            }

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                throw new InvalidOperationException(
                    "The popup canvas transform must be a RectTransform.");
            }

            Vector2 startAnchoredPosition = ResolveAnchorPosition(startAnchor_, canvasRect, root);
            Vector2 endAnchoredPosition = ResolveAnchorPosition(endAnchor_, canvasRect, root);

            if (!IsAlive(root, canvasRect))
            {
                yield break;
            }

            root.anchoredPosition = startAnchoredPosition;
            root.localRotation = Quaternion.Euler(0f, 0f, startRotation_);
            root.localScale = Vector3.one * startScale_;

            if (duration_ <= 0f)
            {
                if (!IsAlive(root, canvasRect))
                {
                    yield break;
                }

                root.anchoredPosition = endAnchoredPosition;
                root.localRotation = Quaternion.Euler(0f, 0f, endRotation_);
                root.localScale = Vector3.one * endScale_;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration_)
            {
                if (!IsAlive(root, canvasRect))
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration_);

                float positionT = positionCurve_.Evaluate(t);
                float rotationT = rotationCurve_.Evaluate(t);
                float scaleT = scaleCurve_.Evaluate(t);

                root.anchoredPosition = Vector2.LerpUnclamped(
                    startAnchoredPosition,
                    endAnchoredPosition,
                    positionT);

                float rotation = Mathf.LerpUnclamped(startRotation_, endRotation_, rotationT);
                root.localRotation = Quaternion.Euler(0f, 0f, rotation);

                float scale = Mathf.LerpUnclamped(startScale_, endScale_, scaleT);
                root.localScale = Vector3.one * scale;

                yield return null;
            }

            if (!IsAlive(root, canvasRect))
            {
                yield break;
            }

            root.anchoredPosition = endAnchoredPosition;
            root.localRotation = Quaternion.Euler(0f, 0f, endRotation_);
            root.localScale = Vector3.one * endScale_;
        }

        private static bool IsAlive(RectTransform root, RectTransform canvasRect)
        {
            return root != null && canvasRect != null;
        }

        private PopupBase ResolvePopup(NavigationTransitionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ActiveNode is not PopupBase popup)
            {
                throw new InvalidOperationException(
                    $"Expected {nameof(context.ActiveNode)} to be a {nameof(PopupBase)}.");
            }

            return popup;
        }

        private Vector2 ResolveAnchorPosition(
            PopupTransitionAnchor anchor,
            RectTransform canvasRect,
            RectTransform popupRect)
        {
            switch (anchor)
            {
                case PopupTransitionAnchor.Center:
                    return Vector2.zero;

                case PopupTransitionAnchor.TopLeftOffscreen:
                    return GetTopLeftOffscreenPosition(canvasRect, popupRect, offscreenPaddingFactor_);

                case PopupTransitionAnchor.BottomRightOffscreen:
                    return GetBottomRightOffscreenPosition(canvasRect, popupRect, offscreenPaddingFactor_);

                default:
                    throw new InvalidOperationException(
                        $"Unsupported popup transition anchor '{anchor}'.");
            }
        }

        private static Vector2 GetTopLeftOffscreenPosition(
            RectTransform canvasRect,
            RectTransform popupRect,
            float paddingFactor)
        {
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 popupSize = popupRect.rect.size;

            float x = -(canvasSize.x * 0.5f + popupSize.x * paddingFactor);
            float y = canvasSize.y * 0.5f + popupSize.y * paddingFactor;

            return new Vector2(x, y);
        }

        private static Vector2 GetBottomRightOffscreenPosition(
            RectTransform canvasRect,
            RectTransform popupRect,
            float paddingFactor)
        {
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 popupSize = popupRect.rect.size;

            float x = canvasSize.x * 0.5f + popupSize.x * paddingFactor;
            float y = -(canvasSize.y * 0.5f + popupSize.y * paddingFactor);

            return new Vector2(x, y);
        }
    }
}
