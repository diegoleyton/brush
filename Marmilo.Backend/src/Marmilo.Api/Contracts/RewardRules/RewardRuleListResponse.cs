namespace Marmilo.Api.Contracts.RewardRules;

public sealed record RewardRuleListResponse(
    IReadOnlyList<RewardRuleResponse> Items);
