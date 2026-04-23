using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Flowbit.Utilities.Unity.UI
{
    /// <summary>
    /// Utility extension methods for <see cref="ScrollRect"/>.
    /// </summary>
    public static class ScrollRectExtensions
    {
        /// <summary>
        /// Smoothly scrolls the content so the target takes the normal top slot position,
        /// matching the position where the first item appears when the list starts.
        /// </summary>
        public static IEnumerator ScrollItemToTopSlotAnimated(
            this ScrollRect scrollRect,
            RectTransform target,
            float time)
        {
            if (scrollRect == null || target == null || scrollRect.content == null)
            {
                yield break;
            }

            RectTransform content = scrollRect.content;
            RectTransform viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();

            float targetY = -target.anchoredPosition.y;
            float topSlotOffset = GetTopSlotOffset(content);
            float desiredY = targetY - topSlotOffset;

            float maxScroll = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            float clampedY = Mathf.Clamp(desiredY, 0f, maxScroll);

            Vector2 startPosition = content.anchoredPosition;
            Vector2 endPosition = new Vector2(startPosition.x, clampedY);

            if (time <= 0f)
            {
                content.anchoredPosition = endPosition;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / time);

                // Smooth easing
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                content.anchoredPosition = Vector2.LerpUnclamped(
                    startPosition,
                    endPosition,
                    easedT);

                yield return null;
            }

            content.anchoredPosition = endPosition;
        }

        /// <summary>
        /// Gets the vertical offset of the first visible item slot from the top of the content.
        /// </summary>
        private static float GetTopSlotOffset(RectTransform content)
        {
            if (content.childCount == 0)
            {
                return 0f;
            }

            RectTransform firstChild = content.GetChild(0) as RectTransform;
            if (firstChild == null)
            {
                return 0f;
            }

            return -firstChild.anchoredPosition.y;
        }
    }
}