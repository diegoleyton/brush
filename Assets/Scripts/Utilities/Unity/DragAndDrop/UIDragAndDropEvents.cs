namespace Flowbit.Utilities.Unity.DragAndDrop
{
    /// <summary>
    /// Reasons why an active drag operation can be cancelled.
    /// </summary>
    public enum DragCancelReason
    {
        Manual,
        Disabled,
        FailedDrop,
        EventSystemCancel
    }
}
