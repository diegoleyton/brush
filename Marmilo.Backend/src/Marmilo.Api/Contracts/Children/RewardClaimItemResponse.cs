namespace Marmilo.Api.Contracts.Children;

public sealed record RewardClaimItemResponse(
    int Kind,
    int RewardType,
    int CurrencyType,
    int Id,
    int Quantity);
