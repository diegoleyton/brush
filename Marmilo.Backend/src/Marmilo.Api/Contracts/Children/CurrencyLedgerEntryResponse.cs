using System.Text.Json;

namespace Marmilo.Api.Contracts.Children;

public sealed record CurrencyLedgerEntryResponse(
    Guid Id,
    string EntryType,
    int Amount,
    Guid? CreatedByParentUserId,
    Guid? RewardRuleId,
    JsonElement Metadata,
    DateTimeOffset CreatedAt);
