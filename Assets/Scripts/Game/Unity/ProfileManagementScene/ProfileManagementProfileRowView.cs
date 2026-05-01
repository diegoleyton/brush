using System;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.ProfileManagementScene
{
    /// <summary>
    /// Visual row used by the profile management screen.
    /// </summary>
    public sealed class ProfileManagementProfileRowView : MonoBehaviour
    {
        [SerializeField]
        private Text label_;

        [SerializeField]
        private Button deleteButton_;

        private Action deleteAction_;

        public void Configure(string label, bool isCurrent, bool canDelete, Action deleteAction)
        {
            if (label_ == null || deleteButton_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ProfileManagementProfileRowView)} is missing one or more required references.");
            }

            label_.text = isCurrent ? $"{label} (Current)" : label;
            deleteButton_.interactable = canDelete;
            deleteAction_ = deleteAction;
        }

        public void DeleteProfile()
        {
            deleteAction_?.Invoke();
        }
    }
}
