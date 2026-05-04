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

        private string loadingMessage_;
        private bool showLoadingWithTime_;
        private float loadingStartedAtRealtime_;

        private void Awake()
        {
            ValidateDependencies();
            BlockScreen(false);
            HideLoading();
        }

        private void Update()
        {
            if (loadingText_ == null || !loadingText_.gameObject.activeSelf)
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
            if (loadingVisualRoot_ != null)
            {
                loadingVisualRoot_.SetActive(true);
            }

            if (loadingText_ == null)
            {
                return;
            }

            loadingMessage_ = string.IsNullOrWhiteSpace(message) ? "Loading..." : message.Trim();
            showLoadingWithTime_ = showLoadingWithTime;
            loadingStartedAtRealtime_ = loadingStartedAtRealtime;
            loadingText_.text = BuildLoadingText();
            loadingText_.gameObject.SetActive(true);
        }

        public void HideLoading()
        {
            loadingMessage_ = string.Empty;
            showLoadingWithTime_ = false;
            loadingStartedAtRealtime_ = 0f;

            if (loadingVisualRoot_ != null)
            {
                loadingVisualRoot_.SetActive(false);
            }

            if (loadingText_ != null)
            {
                loadingText_.text = string.Empty;
                loadingText_.gameObject.SetActive(false);
            }
        }

        private string BuildLoadingText()
        {
            string message = string.IsNullOrWhiteSpace(loadingMessage_) ? "Loading..." : loadingMessage_;
            if (!showLoadingWithTime_)
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
