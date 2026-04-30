using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Game.Unity.ToothBrush
{
    /// <summary>
    /// Drives a fill-based countdown used to visualize brushing progress for a zone.
    /// </summary>
    public class BrushTimer : PausableAnim
    {
        [SerializeField] Image bar_;
        [SerializeField] Image background_;
        [SerializeField] Color lightBackColor_ = Color.white;
        [SerializeField] Color darkBackColor_ = Color.black;

        [SerializeField] float brightnessLimit_ = 0.7f;

        public void StartTimer(float duration)
        {
            StartCoroutine(UpdateBar(duration));
        }

        public void SetColor(Color color)
        {
            bar_.color = color;
            if (background_ != null)
            {
                background_.color = ShouldUseDarkBackground(color) ? darkBackColor_ : lightBackColor_;
            }
        }

        private bool ShouldUseDarkBackground(Color color)
        {
            float brightness = (0.299f * color.r) + (0.587f * color.g) + (0.114f * color.b);
            return brightness >= brightnessLimit_;
        }

        private IEnumerator UpdateBar(float duration)
        {
            if (bar_ == null || duration <= 0f)
                yield break;

            // Save initial scale
            float startScale = 1f;
            float endScale = 0f;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (IsPaused)
                {
                    yield return 0;
                    continue;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Lerp X scale from start to 0
                bar_.fillAmount = Mathf.Lerp(startScale, endScale, t);

                yield return null;
            }

            // Ensure it ends exactly at 0
            bar_.fillAmount = endScale;
        }
    }
}
