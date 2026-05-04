using Marmilo.Api.Development;
using Marmilo.Domain.Parents;
using Marmilo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Marmilo.Api.Auth;

public sealed class CurrentPrincipalContextAccessor : ICurrentPrincipalContextAccessor
{
    private readonly IHttpContextAccessor httpContextAccessor_;
    private readonly MarmiloDbContext dbContext_;
    private readonly IOptions<SupabaseAuthOptions> authOptions_;
    private readonly IHostEnvironment hostEnvironment_;

    public CurrentPrincipalContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        MarmiloDbContext dbContext,
        IOptions<SupabaseAuthOptions> authOptions,
        IHostEnvironment hostEnvironment)
    {
        httpContextAccessor_ = httpContextAccessor;
        dbContext_ = dbContext;
        authOptions_ = authOptions;
        hostEnvironment_ = hostEnvironment;
    }

    public async Task<CurrentPrincipalContext?> TryGetCurrentAsync(CancellationToken cancellationToken)
    {
        HttpContext? httpContext = httpContextAccessor_.HttpContext;
        ClaimsPrincipal? user = httpContext?.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            string? sub = user.FindFirstValue("sub");
            if (Guid.TryParse(sub, out Guid authUserId))
            {
                string? email = user.FindFirstValue("email");
                ParentUser? parentUser = await dbContext_.ParentUsers
                    .FirstOrDefaultAsync(parent => parent.AuthUserId == authUserId, cancellationToken);

                return new CurrentPrincipalContext(
                    authUserId,
                    NormalizeEmail(email),
                    parentUser,
                    IsDevelopmentFallback: false);
            }
        }

        if (!hostEnvironment_.IsDevelopment() || !authOptions_.Value.AllowDevelopmentHeaderFallback)
        {
            return null;
        }

        string? rawValue = httpContext?
            .Request
            .Headers[DevelopmentAuthDefaults.ParentAuthUserIdHeaderName]
            .FirstOrDefault();

        if (!Guid.TryParse(rawValue, out Guid devAuthUserId))
        {
            return null;
        }

        ParentUser? devParentUser = await dbContext_.ParentUsers
            .FirstOrDefaultAsync(parent => parent.AuthUserId == devAuthUserId, cancellationToken);

        return new CurrentPrincipalContext(
            devAuthUserId,
            devParentUser?.Email,
            devParentUser,
            IsDevelopmentFallback: true);
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
