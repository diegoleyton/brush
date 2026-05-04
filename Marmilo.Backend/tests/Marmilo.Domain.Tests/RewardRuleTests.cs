using Marmilo.Domain.Rewards;

namespace Marmilo.Domain.Tests;

public sealed class RewardRuleTests
{
    [Fact]
    public void Constructor_trims_title_and_description()
    {
        RewardRule rule = new(Guid.NewGuid(), "  Compartir  ", "  Ayudar y compartir  ", 5);

        Assert.Equal("Compartir", rule.Title);
        Assert.Equal("Ayudar y compartir", rule.Description);
    }

    [Fact]
    public void Update_rejects_blank_title()
    {
        RewardRule rule = new(Guid.NewGuid(), "Compartir", "Ayudar", 5);

        Assert.Throws<ArgumentException>(() => rule.Update(" ", "Ayudar", 5, true));
    }

    [Fact]
    public void Update_rejects_non_positive_currency_amount()
    {
        RewardRule rule = new(Guid.NewGuid(), "Compartir", "Ayudar", 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => rule.Update("Compartir", "Ayudar", 0, true));
    }
}
