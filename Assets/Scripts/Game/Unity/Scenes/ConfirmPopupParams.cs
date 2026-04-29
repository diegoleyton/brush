using System;
using Flowbit.Utilities.Navigation;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Navigation parameters for the confirm popup.
    /// </summary>
    public sealed class ConfirmPopupParams : NavigationParams
    {
        public ConfirmPopupParams(Action onAccept, Action onCancel)
        {
            OnAccept = onAccept;
            OnCancel = onCancel;
        }

        public Action OnAccept { get; }

        public Action OnCancel { get; }
    }
}
