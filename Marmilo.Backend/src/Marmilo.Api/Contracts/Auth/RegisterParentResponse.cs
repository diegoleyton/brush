namespace Marmilo.Api.Contracts.Auth;

public sealed record RegisterParentResponse(
    ParentSummaryResponse ParentUser,
    FamilySummaryResponse Family,
    string DevelopmentAuthHeaderName,
    string DevelopmentAuthHeaderValue);
