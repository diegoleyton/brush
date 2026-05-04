namespace Marmilo.Api.Contracts.Children;

public sealed record CurrencyLedgerResponse(
    IReadOnlyList<CurrencyLedgerEntryResponse> Items);
