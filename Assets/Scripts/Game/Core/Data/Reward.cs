namespace Game.Core.Data
{
    /// <summary>
    /// Supported reward payload kinds.
    /// </summary>
    public enum RewardKind
    {
        Item,
        Currency
    }

    /// <summary>
    /// Reward granted to the player after completing an action.
    /// </summary>
    public class Reward
    {
        public RewardKind Kind = RewardKind.Item;
        public InteractionPointType RewardType;
        public CurrencyType CurrencyType;
        public int Id;
        public int Quantity;
    }
}
