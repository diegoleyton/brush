using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Localization;
using Flowbit.Utilities.ScreenBlocker;
using Flowbit.Utilities.Unity.UI;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Scenes;
using Game.Unity.UI;

using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.MarketScene
{
    /// <summary>
    /// Drives the market scene, category tabs, purchase confirmations, and purchase feedback.
    /// </summary>
    public sealed class MarketSceneController : SceneBase
    {
        [SerializeField]
        private MarketItemListUI itemList_;

        [SerializeField]
        private MarketTabButtonView[] tabButtons_;

        [SerializeField]
        private Text currencyText_;

        [SerializeField]
        private Text currentProfileText_;

        [SerializeField]
        private FeedbackMessage feedbackMessage_;

        [SerializeField]
        private InteractionPointType selectedItemType_ = InteractionPointType.PLACEABLE_OBJECT;

        private DataRepository repository_;
        private IRoomGameplayService roomGameplayService_;
        private IMarketPurchaseService marketPurchaseService_;
        private EventDispatcher dispatcher_;
        private ScreenBlocker screenBlocker_;
        private bool dispatcherSubscribed_;

        [Inject]
        public void Construct(
            EventDispatcher dispatcher,
            IGameNavigationService navigationService,
            DataRepository repository,
            IRoomGameplayService roomGameplayService,
            IMarketPurchaseService marketPurchaseService,
            ScreenBlocker screenBlocker)
        {
            base.Construct(dispatcher, navigationService);
            dispatcher_ = dispatcher;
            repository_ = repository;
            roomGameplayService_ = roomGameplayService;
            marketPurchaseService_ = marketPurchaseService;
            screenBlocker_ = screenBlocker;
        }

        private void Awake()
        {
            sceneType_ = SceneType.MarketScene;
            ValidateSerializedReferences();
        }

        private void OnEnable()
        {
            if (initialized_)
            {
                Refresh();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        protected override void Initialize()
        {
            SubscribeToDispatcher();
            ClearFeedback();
            BindTabs();
            Refresh();
        }

        public void GoBack()
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.Back();
                return;
            }

            NavigationService.Navigate(SceneType.RoomScene);
        }

        private void OnCurrencyUpdated(CurrencyUpdatedEvent _)
        {
            RefreshCurrency();
        }

        private void OnInventoryUpdated(InventoryUpdatedEvent _)
        {
            RefreshItems();
        }

        private void OnProfileSwitched(ProfileSwitchedEvent _)
        {
            ClearFeedback();
            Refresh();
        }

        private void OnProfileUpdated(ProfileUpdatedEvent _)
        {
            Refresh();
        }

        private void SubscribeToDispatcher()
        {
            if (dispatcherSubscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<CurrencyUpdatedEvent>(OnCurrencyUpdated);
            dispatcher_.Subscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
            dispatcher_.Subscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_.Subscribe<ProfileUpdatedEvent>(OnProfileUpdated);
            dispatcherSubscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!dispatcherSubscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<CurrencyUpdatedEvent>(OnCurrencyUpdated);
            dispatcher_.Unsubscribe<InventoryUpdatedEvent>(OnInventoryUpdated);
            dispatcher_.Unsubscribe<ProfileSwitchedEvent>(OnProfileSwitched);
            dispatcher_.Unsubscribe<ProfileUpdatedEvent>(OnProfileUpdated);
            dispatcherSubscribed_ = false;
        }

        private void BindTabs()
        {
            if (tabButtons_ == null)
            {
                return;
            }

            for (int index = 0; index < tabButtons_.Length; index++)
            {
                MarketTabButtonView tabButton = tabButtons_[index];
                if (tabButton == null)
                {
                    continue;
                }

                InteractionPointType itemType = tabButton.ItemType;
                tabButton.Configure(
                    itemType == selectedItemType_,
                    () => SelectItemType(itemType));
            }
        }

        private void SelectItemType(InteractionPointType itemType)
        {
            if (selectedItemType_ == itemType)
            {
                return;
            }

            selectedItemType_ = itemType;
            ClearFeedback();
            RefreshTabs();
            RefreshItems();
        }

        private void Refresh()
        {
            RefreshTabs();
            RefreshCurrentProfileName();
            RefreshCurrency();
            RefreshItems();
        }

        private void RefreshTabs()
        {
            if (tabButtons_ == null)
            {
                return;
            }

            for (int index = 0; index < tabButtons_.Length; index++)
            {
                MarketTabButtonView tabButton = tabButtons_[index];
                if (tabButton == null)
                {
                    continue;
                }

                InteractionPointType itemType = tabButton.ItemType;
                tabButton.Configure(
                    itemType == selectedItemType_,
                    () => SelectItemType(itemType));
            }
        }

        private void RefreshCurrency()
        {
            if (currencyText_ == null)
            {
                return;
            }

            int coins = repository_?.GetCurrencyBalance(CurrencyType.Coins) ?? 0;
            currencyText_.text = coins.ToString();
        }

        private void RefreshCurrentProfileName()
        {
            if (currentProfileText_ == null)
            {
                return;
            }

            currentProfileText_.text = repository_?.CurrentProfile?.Name ?? string.Empty;
        }

        private void RefreshItems()
        {
            if (itemList_ == null)
            {
                return;
            }

            MarketItemDefinition[] items = GetVisibleItems().ToArray();
            itemList_.SetData(items);
            itemList_.SetSelectionHandler(TryPurchase);
        }

        private IEnumerable<MarketItemDefinition> GetVisibleItems()
        {
            return MarketCatalog.All
                .Where(entry => entry.Key.Item1 == selectedItemType_)
                .OrderBy(entry => entry.Key.Item2)
                .Select(entry => entry.Value);
        }

        private void TryPurchase(MarketItemDefinition definition)
        {
            if (definition == null || repository_ == null || marketPurchaseService_ == null)
            {
                return;
            }

            if (roomGameplayService_ != null &&
                roomGameplayService_.IsMarketItemAlreadyOwned(definition.ItemType, definition.ItemId))
            {
                ShowError(LocalizationServiceLocator.GetText("market.purchase.already_owned", "Item already owned."));
                return;
            }

            if (!repository_.CanAfford(definition.CurrencyType, definition.Price))
            {
                ShowError(LocalizationServiceLocator.GetText("market.purchase.not_enough_currency", "Not enough coins."));
                return;
            }

            NavigationService.Navigate(
                SceneType.ConfirmPopup,
                new ConfirmPopupParams(
                    onAccept: () => _ = PurchaseConfirmedAsync(definition),
                    onCancel: null));
        }

        private async Task PurchaseConfirmedAsync(MarketItemDefinition definition)
        {
            IDisposable blockScope = screenBlocker_?.BlockScope(
                "MarketPurchase",
                showLoadingWithTime: true,
                loadingMessage: LocalizationServiceLocator.GetText(
                    "loading.market.purchase",
                    "Buying item..."));
            try
            {
                MarketPurchaseStatus status = await marketPurchaseService_.PurchaseAsync(definition);
                OnMarketPurchaseCompleted(status);
                Refresh();
            }
            finally
            {
                blockScope?.Dispose();
            }
        }

        private void ClearFeedback()
        {
            feedbackMessage_?.Clear();
        }

        private void ValidateSerializedReferences()
        {
            if (itemList_ == null ||
                currencyText_ == null ||
                currentProfileText_ == null ||
                feedbackMessage_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(MarketSceneController)} is missing one or more required serialized references.");
            }
        }

        private void OnMarketPurchaseCompleted(MarketPurchaseStatus status)
        {
            switch (status)
            {
                case MarketPurchaseStatus.OK:
                    ShowSuccess(LocalizationServiceLocator.GetText("market.purchase.success", "Purchase completed."));
                    return;
                case MarketPurchaseStatus.NOT_ENOUGH_CURRENCY:
                    ShowError(LocalizationServiceLocator.GetText("market.purchase.not_enough_currency", "Not enough coins."));
                    return;
                case MarketPurchaseStatus.ALREADY_OWNED:
                    ShowError(LocalizationServiceLocator.GetText("market.purchase.already_owned", "Item already owned."));
                    return;
                case MarketPurchaseStatus.ITEM_NOT_FOUND:
                    ShowError(LocalizationServiceLocator.GetText("market.purchase.item_not_found", "Item not found."));
                    return;
                case MarketPurchaseStatus.NO_CURRENT_PROFILE:
                    ShowError(LocalizationServiceLocator.GetText("market.purchase.no_profile", "No profile selected."));
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        private void ShowSuccess(string message)
        {
            feedbackMessage_?.ShowSuccess(message, durationSeconds: 2f);
        }

        private void ShowError(string message)
        {
            feedbackMessage_?.ShowError(message, durationSeconds: 2f);
        }
    }
}
