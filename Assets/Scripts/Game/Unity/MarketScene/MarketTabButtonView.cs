using System;

using Game.Core.Data;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.MarketScene
{
    /// <summary>
    /// Represents a serialized market category tab and its selected-state visuals.
    /// </summary>
    public sealed class MarketTabButtonView : MonoBehaviour
    {
        [SerializeField]
        private InteractionPointType itemType_;

        [SerializeField]
        private Button button_;

        [SerializeField]
        private Image background_;

        [SerializeField]
        private Color normalColor_ = Color.white;

        [SerializeField]
        private Color selectedColor_ = Color.white;

        private Action clickAction_;

        public InteractionPointType ItemType => itemType_;

        private void Awake()
        {
            ValidateSerializedReferences();
            button_.onClick.AddListener(Select);
        }

        private void OnDestroy()
        {
            if (button_ != null)
            {
                button_.onClick.RemoveListener(Select);
            }
        }

        public void Configure(bool selected, Action clickAction)
        {
            ValidateSerializedReferences();
            clickAction_ = clickAction;
            background_.color = selected ? selectedColor_ : normalColor_;
        }

        public void Select()
        {
            clickAction_?.Invoke();
        }

        private void ValidateSerializedReferences()
        {
            if (button_ == null || background_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(MarketTabButtonView)} is missing one or more required serialized references.");
            }
        }
    }
}
