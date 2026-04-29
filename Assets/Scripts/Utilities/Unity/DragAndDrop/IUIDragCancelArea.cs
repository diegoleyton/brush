namespace Flowbit.Utilities.Unity.DragAndDrop
{
    /// <summary>
    /// Marker for UI regions that should cancel a drag instead of accepting a drop.
    /// </summary>
    public interface IUIDragCancelArea
    {
        bool CanCancel(UIDraggable draggable);
        void OnDragHoverEntered(UIDraggable draggable);
        void OnDragHoverExited(UIDraggable draggable);
    }
}
