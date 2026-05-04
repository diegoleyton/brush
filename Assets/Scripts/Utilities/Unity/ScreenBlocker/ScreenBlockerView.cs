using UnityEngine;
using UnityEngine.UI;
using System;

namespace Flowbit.Utilities.ScreenBlocker
{
    /// <summary>
    /// Presentation-only view for the global screen blocker.
    /// </summary>
    public class ScreenBlockerView : MonoBehaviour
    {
        [SerializeField]
        private GameObject loadingVisualRoot_;

        [SerializeField]
        private Text loadingText_;

        [SerializeField]
        [Min(0f)]
        private float loadingFeedbackDelaySeconds_ = 0.35f;

        [SerializeField]
        private bool showElapsedSeconds_ = false;

        private string loadingMessage_;
        private bool showLoadingWithTime_;
        private float loadingStartedAtRealtime_;
        private bool loadingVisible_;

        private void Awake()
        {
            ValidateDependencies();
            BlockScreen(false);
            HideLoading();
        }

        private void Update()
        {
            if (!loadingVisible_ && ShouldShowLoadingVisual())
            {
                SetLoadingVisibility(true);
            }

            if (loadingText_ == null || !loadingVisible_ || !loadingText_.gameObject.activeSelf)
            {
                return;
            }

            loadingText_.text = BuildLoadingText();
        }

        public void BlockScreen(bool blocked)
        {
            gameObject.SetActive(blocked);
        }

        public void ShowLoading(string message, bool showLoadingWithTime, float loadingStartedAtRealtime)
        {
            ValidateDependencies();
            loadingMessage_ = string.IsNullOrWhiteSpace(message) ? "Loading..." : message.Trim();
            showLoadingWithTime_ = showLoadingWithTime;
            loadingStartedAtRealtime_ = loadingStartedAtRealtime;
            SetLoadingVisibility(false);

            if (loadingText_ != null && ShouldShowLoadingVisual())
            {
                loadingText_.text = BuildLoadingText();
                SetLoadingVisibility(true);
            }
        }

        public void HideLoading()
        {
            loadingMessage_ = string.Empty;
            showLoadingWithTime_ = false;
            loadingStartedAtRealtime_ = 0f;
            SetLoadingVisibility(false);

            if (loadingText_ != null)
            {
                loadingText_.text = string.Empty;
            }
        }

        private bool ShouldShowLoadingVisual()
        {
            return Time.realtimeSinceStartup - loadingStartedAtRealtime_ >= loadingFeedbackDelaySeconds_;
        }

        private void SetLoadingVisibility(bool visible)
        {
            loadingVisible_ = visible;

            if (loadingVisualRoot_ != null)
            {
                loadingVisualRoot_.SetActive(visible);
            }

            if (loadingText_ != null)
            {
                loadingText_.gameObject.SetActive(visible);
            }
        }

        private string BuildLoadingText()
        {
            string message = string.IsNullOrWhiteSpace(loadingMessage_) ? "Loading..." : loadingMessage_;
            if (!showLoadingWithTime_ || !showElapsedSeconds_)
            {
                return message;
            }

            float elapsedSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - loadingStartedAtRealtime_);
            return $"{message}\n{elapsedSeconds:0.0}s";
        }

        private void ValidateDependencies()
        {
            if (loadingText_ == null || loadingVisualRoot_ == null)
            {
                throw new InvalidOperationException("Missing dependencies inScreenBlockerView.");
            }
        }
    }
}
