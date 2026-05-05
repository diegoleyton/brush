using System;
using System.Threading.Tasks;
using Flowbit.Utilities.RemoteCommunication;
using Flowbit.Utilities.ScreenBlocker;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.UI;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Scene base for screens that must complete a blocking remote bootstrap before becoming safely interactive.
    /// </summary>
    public abstract class RemoteBootstrapSceneBase : SceneBase
    {
        private ScreenBlocker screenBlocker_;
        private bool isRunningRemoteBootstrap_;
        private bool isShowingBlockingPopup_;
        private IDisposable initialEntryBlockScope_;

        protected ScreenBlocker ScreenBlocker => screenBlocker_;

        protected void ConfigureRemoteBootstrap(ScreenBlocker screenBlocker)
        {
            screenBlocker_ = screenBlocker;
        }

        protected void EnsureRemoteBootstrapEntryBlock()
        {
            if (initialEntryBlockScope_ != null || screenBlocker_ == null)
            {
                return;
            }

            initialEntryBlockScope_ = screenBlocker_.BlockScope("RemoteBootstrapEntry");
        }

        protected void DisposeRemoteBootstrapEntryBlock()
        {
            initialEntryBlockScope_?.Dispose();
            initialEntryBlockScope_ = null;
        }

        protected void RunRemoteBootstrap(
            Func<Task> bootstrapAsync,
            string blockerReason,
            string loadingMessage,
            string fallbackFailureMessage,
            string failurePopupTitle,
            string retryPopupTitle)
        {
            _ = RunRemoteBootstrapInternalAsync(
                bootstrapAsync,
                blockerReason,
                loadingMessage,
                fallbackFailureMessage,
                failurePopupTitle,
                retryPopupTitle);
        }

        protected void ShowBlockingRetryPopup(string message, Action onRetry, string title)
        {
            if (NavigationService == null || isShowingBlockingPopup_)
            {
                return;
            }

            isShowingBlockingPopup_ = true;
            NavigationService.Navigate(
                SceneType.RetryPopup,
                new RetryPopupParams(
                    message,
                    onRetry: () =>
                    {
                        isShowingBlockingPopup_ = false;
                        onRetry?.Invoke();
                    },
                    onCancel: HandleBlockingPopupClosed,
                    title: title));
        }

        protected void ShowBlockingErrorPopup(string message, string title)
        {
            if (NavigationService == null || isShowingBlockingPopup_)
            {
                return;
            }

            isShowingBlockingPopup_ = true;
            NavigationService.Navigate(
                SceneType.ErrorPopup,
                new ErrorPopupParams(
                    message,
                    title: title,
                    onClose: HandleBlockingPopupClosed));
        }

        protected virtual void HandleBlockingPopupClosed()
        {
            isShowingBlockingPopup_ = false;
            DisposeRemoteBootstrapEntryBlock();
        }

        protected virtual void OnDestroy()
        {
            DisposeRemoteBootstrapEntryBlock();
        }

        private async Task RunRemoteBootstrapInternalAsync(
            Func<Task> bootstrapAsync,
            string blockerReason,
            string loadingMessage,
            string fallbackFailureMessage,
            string failurePopupTitle,
            string retryPopupTitle)
        {
            if (bootstrapAsync == null || isRunningRemoteBootstrap_)
            {
                return;
            }

            isRunningRemoteBootstrap_ = true;
            IDisposable blockScope = screenBlocker_?.BlockScope(new ScreenBlockerRequest
            {
                Reason = blockerReason,
                ShowLoadingWithTime = true,
                LoadingMessage = loadingMessage,
                ShowLoadingImmediately = true
            });

            try
            {
                await bootstrapAsync();
            }
            catch (AuthSessionInvalidatedException)
            {
                return;
            }
            catch (Exception exception)
            {
                if (!isActiveAndEnabled)
                {
                    return;
                }

                string message = RemoteOperationMessageFormatter.Format(exception, fallbackFailureMessage);
                if (exception is RemoteRequestFailedException remoteException && remoteException.IsNetworkError)
                {
                    ShowBlockingRetryPopup(
                        message,
                        () => RunRemoteBootstrap(
                            bootstrapAsync,
                            blockerReason,
                            loadingMessage,
                            fallbackFailureMessage,
                            failurePopupTitle,
                            retryPopupTitle),
                        retryPopupTitle);
                }
                else
                {
                    ShowBlockingErrorPopup(message, failurePopupTitle);
                }
            }
            finally
            {
                blockScope?.Dispose();
                DisposeRemoteBootstrapEntryBlock();
                isRunningRemoteBootstrap_ = false;
            }
        }
    }
}
