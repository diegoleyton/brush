using Marmilo.Domain.Common;
using Marmilo.Domain.Families;
using Marmilo.Domain.Parents;
using Marmilo.Domain.Rewards;

namespace Marmilo.Domain.Currency;

public sealed class CurrencyLedgerEntry : Entity
{
    private CurrencyLedgerEntry()
    {
    }

    public CurrencyLedgerEntry(
        Guid familyId,
        Guid childProfileId,
        CurrencyLedgerEntryType entryType,
        int amount,
        Guid? createdByParentUserId,
        Guid? rewardRuleId,
        string metadataJson)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Ledger amount must be positive.");
        }

        FamilyId = familyId;
        ChildProfileId = childProfileId;
        EntryType = entryType;
        Amount = amount;
        CreatedByParentUserId = createdByParentUserId;
        RewardRuleId = rewardRuleId;
        MetadataJson = ValidateJson(metadataJson);
    }

    public Guid FamilyId { get; private set; }

    public Guid ChildProfileId { get; private set; }

    public CurrencyLedgerEntryType EntryType { get; private set; }

    public int Amount { get; private set; }

    public Guid? CreatedByParentUserId { get; private set; }

    public Guid? RewardRuleId { get; private set; }

    public string MetadataJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Family Family { get; private set; } = null!;

    public ChildProfile ChildProfile { get; private set; } = null!;

    public ParentUser? CreatedByParentUser { get; private set; }

    public RewardRule? RewardRule { get; private set; }

    private static string ValidateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(json);
            return json;
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new ArgumentException("Invalid JSON payload.", nameof(json), exception);
        }
    }
}
