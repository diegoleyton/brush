using UnityEngine;
using UnityEngine.UI;
using System;

namespace Game.Unity.ProfileSelectionScene
{
    /// <summary>
    /// View for a selectable profile entry inside the profile selection screen.
    /// </summary>
    public sealed class ProfileSelectionButtonView : MonoBehaviour
    {
        [SerializeField]
        private Text label_;

        [SerializeField]
        private Image image_;

        [SerializeField]
        private Color regularColor_;

        [SerializeField]
        private Color highlightColor_;

        private Action selectProfileAction_;

        public void Configure(string label, bool highlighted, Action selectProfileAction)
        {
            label_.text = label;
            selectProfileAction_ = selectProfileAction;
            SetHighlighted(highlighted);
        }

        public void SetHighlighted(bool highlighted)
        {
            image_.color = highlighted ? highlightColor_ : regularColor_;
        }

        public void SelectProfile()
        {
            selectProfileAction_?.Invoke();
        }
    }
}
