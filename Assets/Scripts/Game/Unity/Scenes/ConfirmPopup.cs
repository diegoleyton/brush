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
                Debug.LogWarning("WAAAAAAAAA1 : TryGetConfirmPopupParams");
                onAccept_ = popupParams.OnAccept;
                onCancel_ = popupParams.OnCancel;
            }
        }

        public void Accept()
        {
            Debug.LogWarning("WAAAAAAAAA2 : Accept");
            Resolve(true);
        }

        public void Cancel()
        {
            Debug.LogWarning("WAAAAAAAAA3 : Cancel");
            Resolve(false);
        }

        private void Resolve(bool accepted)
        {
            Debug.LogWarning("WAAAAAAAAA4 : Resolve 1");
            if (resolved_)
            {
                return;
            }

            Debug.LogWarning($"WAAAAAAAAA4 : Resolve 2: {(onAccept_ != null)}, {(onCancel_ != null)}");
            resolved_ = true;
            Action callback = accepted ? onAccept_ : onCancel_;
            Close();
            callback?.Invoke();
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
