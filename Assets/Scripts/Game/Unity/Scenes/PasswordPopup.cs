using System;

using Game.Unity.Definitions;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Popup that gates access behind a simple parent password.
    /// </summary>
    public sealed class PasswordPopup : PopupBase
    {
        private const string ParentPassword = "12345";

        [SerializeField]
        private InputField passwordInput_;

        private Action onAccepted_;
        private Action onCancelled_;
        private bool resolved_;

        private void Awake()
        {
            sceneType_ = SceneType.PasswordPopup;
            ValidateSerializedReferences();
        }

        private void Reset()
        {
            sceneType_ = SceneType.PasswordPopup;
        }

        protected override void Initialize()
        {
            resolved_ = false;
            onAccepted_ = null;
            onCancelled_ = null;

            if (passwordInput_ != null)
            {
                passwordInput_.text = string.Empty;
            }

            if (TryGetPasswordPopupParams(out PasswordPopupParams popupParams))
            {
                onAccepted_ = popupParams.OnAccepted;
                onCancelled_ = popupParams.OnCancelled;
            }
        }

        public void Submit()
        {
            if (resolved_)
            {
                return;
            }

            resolved_ = true;

            if (passwordInput_ != null && passwordInput_.text == ParentPassword)
            {
                onAccepted_?.Invoke();
            }

            Close();
        }

        public void Cancel()
        {
            if (resolved_)
            {
                return;
            }

            resolved_ = true;
            onCancelled_?.Invoke();
            Close();
        }

        private bool TryGetPasswordPopupParams(out PasswordPopupParams popupParams)
        {
            try
            {
                popupParams = GetSceneParameters<PasswordPopupParams>();
                return true;
            }
            catch (ArgumentException)
            {
                popupParams = null;
                return false;
            }
        }

        private void ValidateSerializedReferences()
        {
            if (passwordInput_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(PasswordPopup)} requires a serialized {nameof(InputField)} reference.");
            }
        }
    }
}
