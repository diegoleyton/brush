using System;
using System.Collections;
using Flowbit.Utilities.Localization;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.MarketScene
{
    /// <summary>
    /// Displays market confirmation and failure messages using editor-configured colors.
    /// </summary>
    public sealed class MarketConfirmMessageView : MonoBehaviour
    {
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
            SetMessage(LocalizationServiceLocator.GetText("market.purchase.success", "Purchase completed."), successColor_);
        }

        public void ShowNotEnoughCurrencyMessage()
        {
            SetMessage(LocalizationServiceLocator.GetText("market.purchase.not_enough_currency", "Not enough coins."), failureColor_);
        }

        public void ShowAlreadyOwnedMessage()
        {
            SetMessage(LocalizationServiceLocator.GetText("market.purchase.already_owned", "Item already owned."), failureColor_);
        }

        public void ShowItemNotFoundMessage()
        {
            SetMessage(LocalizationServiceLocator.GetText("market.purchase.item_not_found", "Item not found."), failureColor_);
        }

        public void ShowNoCurrentProfileMessage()
        {
            SetMessage(LocalizationServiceLocator.GetText("market.purchase.no_profile", "No profile selected."), failureColor_);
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
