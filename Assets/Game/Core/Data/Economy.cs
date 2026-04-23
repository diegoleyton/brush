namespace Game.Core.Data
{
    /// <summary>
    /// Currency grant awarded by gameplay or progression systems.
    /// </summary>
    public class CurrencyReward
    {
        public CurrencyType CurrencyType;
        public int Amount;
    }

    /// <summary>
    /// Market item definition exposed to the rest of the domain.
    /// </summary>
    public class MarketItemDefinition
    {
        public InteractionPointType ItemType;
        public int ItemId;
        public CurrencyType CurrencyType;
        public int Price;
        public int Quantity;
    }
}
