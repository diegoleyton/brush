using System;
using Game.Unity.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Close-only popup for displaying blocking or non-blocking errors to the player.
    /// </summary>
    public sealed class ErrorPopup : PopupBase
    {
        [SerializeField]
        private Text titleText_;

        [SerializeField]
        private Text messageText_;

        private Action onClose_;

        private void Awake()
        {
            sceneType_ = SceneType.ErrorPopup;
        }

        private void Reset()
        {
            sceneType_ = SceneType.ErrorPopup;
        }

        protected override void Initialize()
        {
            onClose_ = null;

            if (TryGetErrorPopupParams(out ErrorPopupParams popupParams))
            {
                if (titleText_ != null)
                {
                    titleText_.text = string.IsNullOrWhiteSpace(popupParams.Title) ? "Error" : popupParams.Title;
                }

                if (messageText_ != null)
                {
                    messageText_.text = popupParams.Message ?? string.Empty;
                }

                onClose_ = popupParams.OnClose;
            }
        }

        public void ClosePopup()
        {
            onClose_?.Invoke();
            Close();
        }

        private bool TryGetErrorPopupParams(out ErrorPopupParams popupParams)
        {
            try
            {
                popupParams = GetSceneParameters<ErrorPopupParams>();
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
