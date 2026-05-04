namespace Marmilo.Api.Contracts.Auth;

public sealed record ParentSummaryResponse(
    Guid Id,
    Guid AuthUserId,
    string Email);
