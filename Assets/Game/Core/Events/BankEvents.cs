using Flowbit.Utilities.Core.Events;

using Game.Core.Data;

namespace Game.Core.Events
{
    /// <summary>
    /// Emitted after the currency balance changes.
    /// </summary>
    public sealed class CurrencyUpdatedEvent : IEvent
    {
    }

    /// <summary>
    /// Emitted after a market purchase attempt completes.
    /// </summary>
    public sealed class MarketPurchaseCompletedEvent : IEvent
    {
        public MarketPurchaseCompletedEvent(MarketPurchaseStatus status, MarketItemDefinition item)
        {
            Status = status;
            Item = item;
        }

        public MarketPurchaseStatus Status { get; }
        public MarketItemDefinition Item { get; }
    }
}
