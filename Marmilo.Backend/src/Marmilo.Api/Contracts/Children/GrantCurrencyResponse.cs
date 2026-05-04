namespace Marmilo.Api.Contracts.Children;

public sealed record GrantCurrencyResponse(
    Guid LedgerEntryId,
    int NewBalance);
