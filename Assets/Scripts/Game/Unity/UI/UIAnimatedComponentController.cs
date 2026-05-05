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
        [Serializable]
        private sealed class AnimationProfile
        {
            public Color[] Colors = Array.Empty<Color>();
            public float MinimumScale = 1f;
            public float MaximumScale = 1f;
            public float ScaleCycleDurationSeconds = 1f;
            public float ColorCycleDurationSeconds = 1f;
        }

        [Header("Subtle Animation")]
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

        [Header("Bold Animation")]
        [SerializeField]
        private Color[] boldColors_ =
        {
            new Color(1f, 0.82f, 0.2f, 1f),
            new Color(1f, 0.45f, 0.1f, 1f)
        };

        [SerializeField]
        [Min(0f)]
        private float boldMinimumScale_ = 0.85f;

        [SerializeField]
        [Min(0f)]
        private float boldMaximumScale_ = 1.18f;

        [SerializeField]
        [Min(0f)]
        private float boldScaleCycleDurationSeconds_ = 0.8f;

        [SerializeField]
        [Min(0f)]
        private float boldColorCycleDurationSeconds_ = 1.1f;

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

            AnimationProfile subtleProfile = GetProfile(UIAnimatedComponentAnimationType.Subtle);
            AnimationProfile boldProfile = GetProfile(UIAnimatedComponentAnimationType.Bold);
            float subtleScale = EvaluateScale(subtleProfile);
            Color subtleColor = EvaluateColor(subtleProfile);
            float boldScale = EvaluateScale(boldProfile);
            Color boldColor = EvaluateColor(boldProfile);

            for (int index = components_.Count - 1; index >= 0; index--)
            {
                UIAnimatedComponent component = components_[index];
                if (component == null)
                {
                    components_.RemoveAt(index);
                    continue;
                }

                if (component.AnimationType == UIAnimatedComponentAnimationType.Bold)
                {
                    component.ApplyAnimationState(boldScale, boldColor);
                    continue;
                }

                component.ApplyAnimationState(subtleScale, subtleColor);
            }
        }

        private float EvaluateScale(AnimationProfile profile)
        {
            float duration = profile.ScaleCycleDurationSeconds;
            if (duration <= 0f)
            {
                return profile.MaximumScale;
            }

            float normalizedTime = Mathf.PingPong(Time.unscaledTime / duration, 1f);
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
            return Mathf.Lerp(profile.MinimumScale, profile.MaximumScale, easedTime);
        }

        private Color EvaluateColor(AnimationProfile profile)
        {
            if (profile.Colors.Length == 1)
            {
                return profile.Colors[0];
            }

            float duration = profile.ColorCycleDurationSeconds;
            if (duration <= 0f)
            {
                return profile.Colors[0];
            }

            float totalProgress = Time.unscaledTime / duration;
            int currentIndex = Mathf.FloorToInt(totalProgress) % profile.Colors.Length;
            int nextIndex = (currentIndex + 1) % profile.Colors.Length;
            float blend = totalProgress - Mathf.Floor(totalProgress);
            float easedBlend = Mathf.SmoothStep(0f, 1f, blend);

            return Color.Lerp(profile.Colors[currentIndex], profile.Colors[nextIndex], easedBlend);
        }

        private AnimationProfile GetProfile(UIAnimatedComponentAnimationType animationType)
        {
            switch (animationType)
            {
                case UIAnimatedComponentAnimationType.Bold:
                    return new AnimationProfile
                    {
                        Colors = boldColors_,
                        MinimumScale = boldMinimumScale_,
                        MaximumScale = boldMaximumScale_,
                        ScaleCycleDurationSeconds = boldScaleCycleDurationSeconds_,
                        ColorCycleDurationSeconds = boldColorCycleDurationSeconds_
                    };
                case UIAnimatedComponentAnimationType.Subtle:
                default:
                    return new AnimationProfile
                    {
                        Colors = colors_,
                        MinimumScale = minimumScale_,
                        MaximumScale = maximumScale_,
                        ScaleCycleDurationSeconds = scaleCycleDurationSeconds_,
                        ColorCycleDurationSeconds = colorCycleDurationSeconds_
                    };
            }
        }

        private void ValidateReferences()
        {
            if (settings_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponentController)} requires a {nameof(UISettings)} binding.");
            }

            ValidateProfile(GetProfile(UIAnimatedComponentAnimationType.Subtle), "subtle animation");
            ValidateProfile(GetProfile(UIAnimatedComponentAnimationType.Bold), "bold animation");
        }

        private static void ValidateProfile(AnimationProfile profile, string profileName)
        {
            if (profile == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponentController)} requires a {profileName} configuration.");
            }

            if (profile.MinimumScale > profile.MaximumScale)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponentController)} requires {profileName} minimum scale to be less than or equal to maximum scale.");
            }

            if (profile.Colors == null || profile.Colors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponentController)} requires at least one color in {profileName}.");
            }
        }
    }
}
