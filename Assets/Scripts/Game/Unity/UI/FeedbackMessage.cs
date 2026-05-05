using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.UI
{
    /// <summary>
    /// Reusable timed message presenter for lightweight UI feedback.
    /// </summary>
    public sealed class FeedbackMessage : MonoBehaviour
    {
        [SerializeField]
        private Text text_;

        [SerializeField]
        private Color infoColor_ = Color.white;

        [SerializeField]
        private Color successColor_ = Color.white;

        [SerializeField]
        private Color errorColor_ = Color.white;

        [SerializeField]
        [Min(0f)]
        private float defaultDisplayDurationSeconds_ = 0f;

        private Coroutine hideCoroutine_;

        private void Awake()
        {
            ValidateSerializedReferences();
            Clear();
        }

        private void OnDisable()
        {
            StopHideCoroutine();
        }

        public void ShowInfo(string message, float? durationSeconds = null)
        {
            Show(message, infoColor_, durationSeconds);
        }

        public void ShowSuccess(string message, float? durationSeconds = null)
        {
            Show(message, successColor_, durationSeconds);
        }

        public void ShowError(string message, float? durationSeconds = null)
        {
            Show(message, errorColor_, durationSeconds);
        }

        public void Clear()
        {
            StopHideCoroutine();

            if (text_ == null)
            {
                return;
            }

            text_.text = string.Empty;
            text_.gameObject.SetActive(false);
        }

        private void Show(string message, Color color, float? durationSeconds)
        {
            if (text_ == null)
            {
                return;
            }

            StopHideCoroutine();
            text_.text = message ?? string.Empty;
            text_.color = color;
            text_.gameObject.SetActive(!string.IsNullOrWhiteSpace(text_.text));

            float resolvedDuration = durationSeconds ?? defaultDisplayDurationSeconds_;
            if (resolvedDuration > 0f)
            {
                hideCoroutine_ = StartCoroutine(HideAfterDelay(resolvedDuration));
            }
        }

        private IEnumerator HideAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            hideCoroutine_ = null;
            Clear();
        }

        private void StopHideCoroutine()
        {
            if (hideCoroutine_ == null)
            {
                return;
            }

            StopCoroutine(hideCoroutine_);
            hideCoroutine_ = null;
        }

        private void ValidateSerializedReferences()
        {
            if (text_ == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(FeedbackMessage)} requires a serialized {nameof(Text)} reference.");
            }
        }
    }
}
