using Marmilo.Domain.Common;
using Marmilo.Domain.Families;
using Marmilo.Domain.Market;
using Marmilo.Domain.Parents;

namespace Marmilo.Domain.Redemptions;

public sealed class Redemption : Entity
{
    private Redemption()
    {
    }

    public Redemption(
        Guid familyId,
        Guid childProfileId,
        Guid marketItemId,
        int cost)
    {
        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Redemption cost cannot be negative.");
        }

        FamilyId = familyId;
        ChildProfileId = childProfileId;
        MarketItemId = marketItemId;
        Cost = cost;
        Status = RedemptionStatus.Requested;
    }

    public Guid FamilyId { get; private set; }

    public Guid ChildProfileId { get; private set; }

    public Guid MarketItemId { get; private set; }

    public int Cost { get; private set; }

    public RedemptionStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ResolvedAt { get; private set; }

    public Guid? ResolvedByParentUserId { get; private set; }

    public Family Family { get; private set; } = null!;

    public ChildProfile ChildProfile { get; private set; } = null!;

    public MarketItem MarketItem { get; private set; } = null!;

    public ParentUser? ResolvedByParentUser { get; private set; }

    public void Resolve(RedemptionStatus status, Guid? resolvedByParentUserId)
    {
        if (status == RedemptionStatus.Requested)
        {
            throw new ArgumentException("Requested is not a valid resolved status.", nameof(status));
        }

        Status = status;
        ResolvedByParentUserId = resolvedByParentUserId;
        ResolvedAt = DateTimeOffset.UtcNow;
    }
}
