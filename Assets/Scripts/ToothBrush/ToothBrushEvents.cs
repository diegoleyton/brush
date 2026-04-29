using Flowbit.Utilities.Core.Events;

public enum InteractionEventType
{
    BRUSH_STARTED,
    BRUSH_ENDED,
    BRUSH_ZONE_READY
}

public sealed class InteractionEvent : IEvent
{
    public InteractionEvent(InteractionEventType interactionEventType)
    {
        InteractionEventType = interactionEventType;
    }

    public InteractionEventType InteractionEventType { get; }
}

public sealed class BrushPausedEvent : IEvent
{
    public BrushPausedEvent(bool isPaused)
    {
        IsPaused = isPaused;
    }

    public bool IsPaused { get; }
}
