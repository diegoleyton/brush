using System;
using Game.Unity.Definitions;
using UnityEngine;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Simple confirmation popup driven by navigation parameters.
    /// </summary>
    public sealed class ConfirmPopup : PopupBase
    {
        private Action onAccept_;
        private Action onCancel_;
        private bool resolved_;

        private void Awake()
        {
            sceneType_ = SceneType.ConfirmPopup;
        }

        private void Reset()
        {
            sceneType_ = SceneType.ConfirmPopup;
        }

        protected override void Initialize()
        {
            resolved_ = false;
            onAccept_ = null;
            onCancel_ = null;

            if (TryGetConfirmPopupParams(out ConfirmPopupParams popupParams))
            {
                onAccept_ = popupParams.OnAccept;
                onCancel_ = popupParams.OnCancel;
            }
        }

        public void Accept()
        {
            Resolve(true);
        }

        public void Cancel()
        {
            Resolve(false);
        }

        private void Resolve(bool accepted)
        {
            if (resolved_)
            {
                return;
            }

            resolved_ = true;
            Action callback = accepted ? onAccept_ : onCancel_;
            callback?.Invoke();
            Close();
        }

        private bool TryGetConfirmPopupParams(out ConfirmPopupParams popupParams)
        {
            try
            {
                popupParams = GetSceneParameters<ConfirmPopupParams>();
                return true;
            }
            catch (ArgumentException)
            {
                popupParams = null;
                return false;
            }
        }
    }
}
