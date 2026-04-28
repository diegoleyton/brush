using System;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Unity.UI
{
    /// <summary>
    /// Drives all registered animated UI components in a synchronized loop.
    /// </summary>
    public sealed class UIAnimatedComponentController : MonoBehaviour
    {
        [SerializeField]
        private Color[] colors_ = Array.Empty<Color>();

        [SerializeField]
        [Min(0f)]
        private float minimumScale_ = 0.92f;

        [SerializeField]
        [Min(0f)]
        private float maximumScale_ = 1.08f;

        [SerializeField]
        [Min(0f)]
        private float scaleCycleDurationSeconds_ = 1.25f;

        [SerializeField]
        [Min(0f)]
        private float colorCycleDurationSeconds_ = 2f;

        private readonly List<UIAnimatedComponent> components_ = new List<UIAnimatedComponent>();

        private UISettings settings_;

        [Zenject.Inject]
        public void Construct(UISettings settings)
        {
            settings_ = settings;
        }

        private void Awake()
        {
            ValidateReferences();
        }

        public void Register(UIAnimatedComponent component)
        {
            if (component == null || components_.Contains(component))
            {
                return;
            }

            components_.Add(component);
        }

        public void Unregister(UIAnimatedComponent component)
        {
            if (component == null)
            {
                return;
            }

            components_.Remove(component);
        }

        private void Update()
        {
            if (components_.Count == 0)
            {
                return;
            }

            float scale = EvaluateScale();
            Color color = EvaluateColor();

            for (int index = components_.Count - 1; index >= 0; index--)
            {
                UIAnimatedComponent component = components_[index];
                if (component == null)
                {
                    components_.RemoveAt(index);
                    continue;
                }

                component.ApplyAnimationState(scale, color);
            }
        }

        private float EvaluateScale()
        {
            float duration = scaleCycleDurationSeconds_;
            if (duration <= 0f)
            {
                return maximumScale_;
            }

            float normalizedTime = Mathf.PingPong(Time.unscaledTime / duration, 1f);
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            return Mathf.Lerp(minimumScale_, maximumScale_, easedTime);
        }

        private Color EvaluateColor()
        {
            if (colors_.Length == 1)
            {
                return colors_[0];
            }

            float duration = colorCycleDurationSeconds_;
            if (duration <= 0f)
            {
                return colors_[0];
            }

            float totalProgress = Time.unscaledTime / duration;
            int currentIndex = Mathf.FloorToInt(totalProgress) % colors_.Length;
            int nextIndex = (currentIndex + 1) % colors_.Length;
            float blend = totalProgress - Mathf.Floor(totalProgress);
            float easedBlend = Mathf.SmoothStep(0f, 1f, blend);

            return Color.Lerp(colors_[currentIndex], colors_[nextIndex], easedBlend);
        }

        private void ValidateReferences()
        {
            if (settings_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponentController)} requires a {nameof(UISettings)} binding.");
            }

            if (minimumScale_ > maximumScale_)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponentController)} requires {nameof(minimumScale_)} to be less than or equal to {nameof(maximumScale_)}.");
            }

            if (colors_ == null || colors_.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponentController)} requires at least one color.");
            }
        }
    }
}
