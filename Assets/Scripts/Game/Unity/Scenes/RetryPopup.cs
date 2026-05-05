using System;
using Game.Unity.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Popup for blocking transport failures that offers retry and cancel actions.
    /// </summary>
    public sealed class RetryPopup : PopupBase
    {
        [SerializeField]
        private Text titleText_;

        [SerializeField]
        private Text messageText_;

        private Action onRetry_;
        private Action onCancel_;
        private bool resolved_;

        private void Awake()
        {
            sceneType_ = SceneType.RetryPopup;
        }

        private void Reset()
        {
            sceneType_ = SceneType.RetryPopup;
        }

        protected override void Initialize()
        {
            resolved_ = false;
            onRetry_ = null;
            onCancel_ = null;

            if (TryGetRetryPopupParams(out RetryPopupParams popupParams))
            {
                if (titleText_ != null)
                {
                    titleText_.text = string.IsNullOrWhiteSpace(popupParams.Title) ? "Connection problem" : popupParams.Title;
                }

                if (messageText_ != null)
                {
                    messageText_.text = popupParams.Message ?? string.Empty;
                }

                onRetry_ = popupParams.OnRetry;
                onCancel_ = popupParams.OnCancel;
            }
        }

        public void Retry()
        {
            Resolve(retry: true);
        }

        public void Cancel()
        {
            Resolve(retry: false);
        }

        private void Resolve(bool retry)
        {
            if (resolved_)
            {
                return;
            }

            resolved_ = true;
            Action callback = retry ? onRetry_ : onCancel_;
            callback?.Invoke();
            Close();
        }

        private bool TryGetRetryPopupParams(out RetryPopupParams popupParams)
        {
            try
            {
                popupParams = GetSceneParameters<RetryPopupParams>();
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
