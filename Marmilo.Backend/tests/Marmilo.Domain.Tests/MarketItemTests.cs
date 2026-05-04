using Marmilo.Domain.Market;

namespace Marmilo.Domain.Tests;

public sealed class MarketItemTests
{
    [Fact]
    public void Constructor_rejects_negative_price()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MarketItem(Guid.NewGuid(), "Helado", "Salida", -1, MarketItemType.RealWorldReward, "{}"));
    }

    [Fact]
    public void Constructor_normalizes_blank_payload_to_empty_object()
    {
        MarketItem item = new(Guid.NewGuid(), "Helado", "Salida", 5, MarketItemType.RealWorldReward, "");

        Assert.Equal("{}", item.PayloadJson);
    }

    [Fact]
    public void Update_rejects_invalid_json_payload()
    {
        MarketItem item = new(Guid.NewGuid(), "Helado", "Salida", 5, MarketItemType.RealWorldReward, "{}");

        Assert.Throws<ArgumentException>(() =>
            item.Update("Helado", "Salida", 5, MarketItemType.RealWorldReward, "{", true));
    }
}
