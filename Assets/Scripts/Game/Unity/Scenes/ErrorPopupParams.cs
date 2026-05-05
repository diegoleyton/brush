using System;
using Flowbit.Utilities.Navigation;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Navigation parameters for a simple close-only error popup.
    /// </summary>
    public sealed class ErrorPopupParams : NavigationParams
    {
        public ErrorPopupParams(string message, string title = null, Action onClose = null)
        {
            Message = message ?? string.Empty;
            Title = title ?? string.Empty;
            OnClose = onClose;
        }

        public string Message { get; }

        public string Title { get; }

        public Action OnClose { get; }
    }
}
