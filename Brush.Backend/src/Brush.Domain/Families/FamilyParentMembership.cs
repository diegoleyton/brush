using Brush.Domain.Parents;

namespace Brush.Domain.Families;

public sealed class FamilyParentMembership
{
    private FamilyParentMembership()
    {
    }

    public FamilyParentMembership(Guid familyId, Guid parentUserId, FamilyParentRole role)
    {
        FamilyId = familyId;
        ParentUserId = parentUserId;
        Role = role;
    }

    public Guid FamilyId { get; private set; }

    public Guid ParentUserId { get; private set; }

    public FamilyParentRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Family Family { get; private set; } = null!;

    public ParentUser ParentUser { get; private set; } = null!;
}
