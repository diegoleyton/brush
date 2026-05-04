namespace Marmilo.Api.Contracts.RewardRules;

public sealed record RewardRuleResponse(
    Guid Id,
    string Title,
    string Description,
    int CurrencyAmount,
    bool IsActive);
