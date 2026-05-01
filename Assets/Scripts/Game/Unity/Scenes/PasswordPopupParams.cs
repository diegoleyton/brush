using System;

using Flowbit.Utilities.Navigation;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Navigation parameters for the password popup.
    /// </summary>
    public sealed class PasswordPopupParams : NavigationParams
    {
        public PasswordPopupParams(Action onAccepted, Action onCancelled)
        {
            OnAccepted = onAccepted;
            OnCancelled = onCancelled;
        }

        public Action OnAccepted { get; }

        public Action OnCancelled { get; }
    }
}
