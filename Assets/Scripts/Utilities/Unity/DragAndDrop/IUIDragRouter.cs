namespace Flowbit.Utilities.Unity.DragAndDrop
{
    /// <summary>
    /// Represents a component that takes ownership of drag gesture routing for a UIDraggable.
    /// </summary>
    public interface IUIDragRouter
    {
        /// <summary>
        /// Gets whether this router is currently active and should own drag gesture routing.
        /// </summary>
        bool IsRoutingEnabled { get; }
    }
}
