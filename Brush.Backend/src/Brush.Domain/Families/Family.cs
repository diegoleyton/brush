using Brush.Domain.Common;
using Brush.Domain.Parents;

namespace Brush.Domain.Families;

public sealed class Family : Entity
{
    private readonly List<FamilyParentMembership> parentMemberships_ = new();
    private readonly List<ChildProfile> childProfiles_ = new();

    private Family()
    {
    }

    public Family(string name, Guid createdByParentUserId)
    {
        CreatedByParentUserId = createdByParentUserId;
        Rename(name);
    }

    public string Name { get; private set; } = string.Empty;

    public Guid CreatedByParentUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public IReadOnlyCollection<FamilyParentMembership> ParentMemberships => parentMemberships_;

    public IReadOnlyCollection<ChildProfile> ChildProfiles => childProfiles_;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Family name is required.", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
