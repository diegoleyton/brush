using System;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.MarketScene
{
    /// <summary>
    /// Displays market confirmation and failure messages using editor-configured colors.
    /// </summary>
    public sealed class MarketConfirmMessageView : MonoBehaviour
    {
        private const string SuccessMessage = "Purchase completed.";
        private const string NotEnoughCurrencyMessage = "Not enough coins.";
        private const string AlreadyOwnedMessage = "Item already owned.";
        private const string ItemNotFoundMessage = "Item not found.";
        private const string NoCurrentProfileMessage = "No profile selected.";

        [SerializeField]
        private Text messageText_;

        [SerializeField]
        private Color successColor_ = Color.white;

        [SerializeField]
        private Color failureColor_ = Color.white;

        [SerializeField]
        [Min(0f)]
        private float displayDurationSeconds_ = 2f;

        private Coroutine hideCoroutine_;

        private void Awake()
        {
            if (messageText_ != null)
            {
                messageText_.text = string.Empty;
                messageText_.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            StopHideCoroutine();
        }

        public void Validate()
        {
            if (messageText_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(MarketConfirmMessageView)} is missing one or more required serialized references.");
            }
        }

        public void ShowSuccessMessage()
        {
            SetMessage(SuccessMessage, successColor_);
        }

        public void ShowNotEnoughCurrencyMessage()
        {
            SetMessage(NotEnoughCurrencyMessage, failureColor_);
        }

        public void ShowAlreadyOwnedMessage()
        {
            SetMessage(AlreadyOwnedMessage, failureColor_);
        }

        public void ShowItemNotFoundMessage()
        {
            SetMessage(ItemNotFoundMessage, failureColor_);
        }

        public void ShowNoCurrentProfileMessage()
        {
            SetMessage(NoCurrentProfileMessage, failureColor_);
        }

        public void Clear()
        {
            StopHideCoroutine();
            messageText_.text = string.Empty;
            messageText_.gameObject.SetActive(false);
        }

        private void SetMessage(string message, Color color)
        {
            Validate();
            StopHideCoroutine();
            messageText_.text = message;
            messageText_.color = color;
            messageText_.gameObject.SetActive(true);
            hideCoroutine_ = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            if (displayDurationSeconds_ > 0f)
            {
                yield return new WaitForSeconds(displayDurationSeconds_);
            }

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
    }
}
