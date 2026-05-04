namespace Marmilo.Api.Contracts.RewardRules;

public sealed record UpdateRewardRuleRequest(
    string Title,
    string Description,
    int CurrencyAmount,
    bool IsActive);
