using Marmilo.Domain.Common;
using Marmilo.Domain.Families;

namespace Marmilo.Domain.Rewards;

public sealed class RewardRule : Entity
{
    private RewardRule()
    {
    }

    public RewardRule(Guid familyId, string title, string description, int currencyAmount)
    {
        FamilyId = familyId;
        Update(title, description, currencyAmount, isActive: true);
    }

    public Guid FamilyId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public int CurrencyAmount { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Family Family { get; private set; } = null!;

    public void Update(string title, string description, int currencyAmount, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Reward rule title is required.", nameof(title));
        }

        if (currencyAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currencyAmount), "Currency amount must be positive.");
        }

        Title = title.Trim();
        Description = (description ?? string.Empty).Trim();
        CurrencyAmount = currencyAmount;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
