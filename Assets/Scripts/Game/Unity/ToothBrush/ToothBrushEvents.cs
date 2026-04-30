using Flowbit.Utilities.Core.Events;

namespace Game.Unity.ToothBrush
{
    /// <summary>
    /// Enumerates the gameplay interaction milestones emitted during toothbrushing.
    /// </summary>
    public enum InteractionEventType
    {
        BRUSH_STARTED,
        BRUSH_ENDED,
        BRUSH_ZONE_READY
    }

    /// <summary>
    /// Event raised when the toothbrushing interaction changes state.
    /// </summary>
    public sealed class InteractionEvent : IEvent
    {
        public InteractionEvent(InteractionEventType interactionEventType)
        {
            InteractionEventType = interactionEventType;
        }

        public InteractionEventType InteractionEventType { get; }
    }

    /// <summary>
    /// Event raised when the toothbrush flow is paused or resumed.
    /// </summary>
    public sealed class BrushPausedEvent : IEvent
    {
        public BrushPausedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }

        public bool IsPaused { get; }
    }
}
