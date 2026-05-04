namespace Marmilo.Api.Contracts.RewardRules;

public sealed record CreateRewardRuleRequest(
    string Title,
    string Description,
    int CurrencyAmount);
