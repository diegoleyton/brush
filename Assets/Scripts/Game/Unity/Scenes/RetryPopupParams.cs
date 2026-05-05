using System;
using Flowbit.Utilities.Navigation;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Navigation parameters for a retry/cancel popup used by blocking transport failures.
    /// </summary>
    public sealed class RetryPopupParams : NavigationParams
    {
        public RetryPopupParams(
            string message,
            Action onRetry,
            Action onCancel = null,
            string title = null)
        {
            Message = message ?? string.Empty;
            Title = title ?? string.Empty;
            OnRetry = onRetry;
            OnCancel = onCancel;
        }

        public string Message { get; }

        public string Title { get; }

        public Action OnRetry { get; }

        public Action OnCancel { get; }
    }
}
