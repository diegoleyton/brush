using Flowbit.Utilities.Unity.UI;
using Flowbit.Utilities.Core.Events;

using Game.Core.Data;
using Game.Unity.Definitions.Events;

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;

namespace Game.Unity.RoomScene
{
    [Serializable]
    public sealed class RoomInventoryCategoryButtonBinding
    {
        public InteractionPointType InteractionPointType = InteractionPointType.PLACEABLE_OBJECT;
        public Button Button;
        public Image Image;

        [NonSerialized]
        public UnityEngine.Events.UnityAction ClickAction;

        public Image ResolveImage()
        {
            if (Image != null)
            {
                return Image;
            }

            if (Button == null)
            {
                return null;
            }

            if (Button.targetGraphic is Image targetGraphicImage)
            {
                return targetGraphicImage;
            }

            return null;
        }
    }

    /// <summary>
    /// Handles the view of the inventory in the room
    /// </summary>
    public sealed class RoomInventoryView : MonoBehaviour
    {
        private EventDispatcher dispatcher_;

        [SerializeField]
        private UIComponentAnimatorController animatorController_;

        [SerializeField]
        [Min(0f)]
        private float showDelaySeconds_ = 0.5f;

        [SerializeField]
        private Color selectedColor_ = Color.white;

        [SerializeField]
        private Color deselectedColor_ = Color.gray;

        [SerializeField]
        private RoomInventoryCategoryButtonBinding[] categoryButtons_ = Array.Empty<RoomInventoryCategoryButtonBinding>();

        private bool hidden_ = true;
        private Coroutine showCoroutine_;

        public bool IsHidden => hidden_;

        public IReadOnlyList<RoomInventoryCategoryButtonBinding> CategoryButtons => categoryButtons_;

        [Zenject.Inject]
        public void Construct(EventDispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        private void Awake()
        {
            ValidateReferences();
        }

        public void Toggle()
        {
            if (hidden_)
            {
                Show();
                return;
            }

            Hide();
        }

        public void Show()
        {
            CancelPendingShow();

            if (!hidden_)
            {
                return;
            }

            hidden_ = false;
            animatorController_.GoToInitialState();
            dispatcher_?.Send(new RoomInventoryOpenedEvent());
        }

        public void ShowWithDelay()
        {
            CancelPendingShow();
            showCoroutine_ = StartCoroutine(ShowAfterDelay());
        }

        public void Hide()
        {
            CancelPendingShow();

            if (hidden_)
            {
                return;
            }

            hidden_ = true;
            animatorController_.GoToFinalState();
            dispatcher_?.Send(new RoomInventoryClosedEvent());
        }

        public void SetSelectedCategory(InteractionPointType selectedInteractionPointType)
        {
            if (categoryButtons_ == null)
            {
                return;
            }

            for (int index = 0; index < categoryButtons_.Length; index++)
            {
                RoomInventoryCategoryButtonBinding binding = categoryButtons_[index];
                if (binding == null)
                {
                    continue;
                }

                bool isSelected = binding.InteractionPointType == selectedInteractionPointType;

                if (binding.Button != null)
                {
                    binding.Button.interactable = !isSelected;
                }

                Image buttonImage = binding.ResolveImage();
                if (buttonImage == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(RoomInventoryView)} requires an Image or Image targetGraphic for category button '{binding.Button?.name}'.");
                }

                buttonImage.color = isSelected ? selectedColor_ : deselectedColor_;
            }
        }

        private IEnumerator ShowAfterDelay()
        {
            if (showDelaySeconds_ > 0f)
            {
                yield return new WaitForSeconds(showDelaySeconds_);
            }

            Show();
            showCoroutine_ = null;
        }

        private void CancelPendingShow()
        {
            if (showCoroutine_ == null)
            {
                return;
            }

            StopCoroutine(showCoroutine_);
            showCoroutine_ = null;
        }

        private void ValidateReferences()
        {
            if (animatorController_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomInventoryView)} requires a {nameof(UIComponentAnimatorController)} reference.");
            }

            if (categoryButtons_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomInventoryView)} requires category button bindings.");
            }

            for (int index = 0; index < categoryButtons_.Length; index++)
            {
                RoomInventoryCategoryButtonBinding binding = categoryButtons_[index];
                if (binding == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(RoomInventoryView)} has a missing category button binding at index {index}.");
                }

                if (binding.Button == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(RoomInventoryView)} category button binding at index {index} requires a Button reference.");
                }

                if (binding.ResolveImage() == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(RoomInventoryView)} category button '{binding.Button.name}' requires an explicit Image or Image targetGraphic.");
                }
            }
        }
    }
}
