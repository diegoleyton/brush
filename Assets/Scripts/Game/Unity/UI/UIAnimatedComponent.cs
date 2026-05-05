using System;

using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.UI
{
    /// <summary>
    /// Visual UI element driven by the shared animated UI controller.
    /// </summary>
    public sealed class UIAnimatedComponent : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        [SerializeField]
        private UIAnimatedComponentAnimationType animationType_ = UIAnimatedComponentAnimationType.Subtle;

        private RectTransform animatedTransform_;

        private UIAnimatedComponentController controller_;

        public UIAnimatedComponentAnimationType AnimationType => animationType_;

        [Inject]
        public void Construct(UIAnimatedComponentController controller)
        {
            controller_ = controller;
        }

        private void Awake()
        {
            animatedTransform_ = transform as RectTransform;

            if (image_ == null)
            {
                image_ = GetComponent<Image>();
            }

            ValidateReferences();
        }

        private void OnEnable()
        {
            controller_?.Register(this);
        }

        private void OnDisable()
        {
            controller_?.Unregister(this);
        }

        public void ApplyAnimationState(float uniformScale, Color color)
        {
            animatedTransform_.localScale = new Vector3(uniformScale, uniformScale, 1f);
            image_.color = color;
        }

        private void ValidateReferences()
        {
            if (animatedTransform_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponent)} requires a RectTransform on the same GameObject.");
            }

            if (image_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIAnimatedComponent)} requires an {nameof(Image)} reference or an {nameof(Image)} on the same GameObject.");
            }
        }
    }
}
