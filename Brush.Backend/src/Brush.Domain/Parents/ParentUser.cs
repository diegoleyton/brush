using Brush.Domain.Common;
using Brush.Domain.Families;

namespace Brush.Domain.Parents;

public sealed class ParentUser : Entity
{
    private readonly List<FamilyParentMembership> familyMemberships_ = new();

    private ParentUser()
    {
    }

    public ParentUser(Guid authUserId, string email)
    {
        AuthUserId = authUserId;
        UpdateEmail(email);
    }

    public Guid AuthUserId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public IReadOnlyCollection<FamilyParentMembership> FamilyMemberships => familyMemberships_;

    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Email = email.Trim().ToLowerInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
