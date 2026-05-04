using System.Text.Json;

namespace Marmilo.Api.Contracts.Children;

public sealed record GrantCurrencyRequest(
    int? Amount,
    Guid? RewardRuleId,
    string? Reason,
    JsonElement? Metadata);
