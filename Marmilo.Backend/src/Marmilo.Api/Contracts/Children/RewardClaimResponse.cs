namespace Marmilo.Api.Contracts.Children;

public sealed record RewardClaimResponse(
    IReadOnlyList<RewardClaimItemResponse> Rewards);
