using UnityEngine;
using UnityEngine.UI;

namespace Flowbit.Utilities.ScreenBlocker
{
    /// <summary>
    /// Holds the image used by the global screen blocker service.
    /// </summary>
    public class ScreenBlockerView : MonoBehaviour
    {
        [field: SerializeField]
        public Image Image { get; private set; }

        [SerializeField]
        private Text loadingText_;

        private string loadingMessage_;
        private bool showLoadingWithTime_;
        private float loadingStartedAtRealtime_;

        private void Awake()
        {
            EnsureLoadingText();
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

        public void ShowLoading(string message, bool showLoadingWithTime, float loadingStartedAtRealtime)
        {
            EnsureLoadingText();
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

        private void EnsureLoadingText()
        {
            if (loadingText_ != null)
            {
                return;
            }

            if (Image == null)
            {
                return;
            }

            GameObject loadingTextObject = new GameObject("LoadingText", typeof(RectTransform), typeof(Text));
            loadingTextObject.transform.SetParent(Image.transform, false);

            RectTransform rectTransform = loadingTextObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(32f, 32f);
            rectTransform.offsetMax = new Vector2(-32f, -32f);

            loadingText_ = loadingTextObject.GetComponent<Text>();
            loadingText_.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            loadingText_.fontSize = 42;
            loadingText_.alignment = TextAnchor.MiddleCenter;
            loadingText_.color = Color.white;
            loadingText_.horizontalOverflow = HorizontalWrapMode.Wrap;
            loadingText_.verticalOverflow = VerticalWrapMode.Overflow;
            loadingText_.raycastTarget = false;
        }
    }
}
