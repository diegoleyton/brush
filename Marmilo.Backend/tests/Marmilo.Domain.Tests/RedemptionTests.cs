using Marmilo.Domain.Redemptions;

namespace Marmilo.Domain.Tests;

public sealed class RedemptionTests
{
    [Fact]
    public void Constructor_rejects_negative_cost()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Redemption(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1));
    }

    [Fact]
    public void Resolve_rejects_requested_status()
    {
        Redemption redemption = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5);

        Assert.Throws<ArgumentException>(() =>
            redemption.Resolve(RedemptionStatus.Requested, Guid.NewGuid()));
    }

    [Fact]
    public void Resolve_sets_status_parent_and_timestamp()
    {
        Redemption redemption = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5);
        Guid parentUserId = Guid.NewGuid();

        redemption.Resolve(RedemptionStatus.Fulfilled, parentUserId);

        Assert.Equal(RedemptionStatus.Fulfilled, redemption.Status);
        Assert.Equal(parentUserId, redemption.ResolvedByParentUserId);
        Assert.NotNull(redemption.ResolvedAt);
    }
}
