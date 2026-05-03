using Brush.Infrastructure.Persistence;
using Brush.Domain.Parents;
using Microsoft.EntityFrameworkCore;

namespace Brush.Api.Development;

public sealed class HeaderCurrentParentContext : ICurrentParentContext
{
    private readonly IHttpContextAccessor httpContextAccessor_;
    private readonly BrushDbContext dbContext_;

    public HeaderCurrentParentContext(
        IHttpContextAccessor httpContextAccessor,
        BrushDbContext dbContext)
    {
        httpContextAccessor_ = httpContextAccessor;
        dbContext_ = dbContext;
    }

    public Task<ParentUser?> TryGetCurrentParentAsync(CancellationToken cancellationToken)
    {
        string? rawValue = httpContextAccessor_.HttpContext?
            .Request
            .Headers[DevelopmentAuthDefaults.ParentAuthUserIdHeaderName]
            .FirstOrDefault();

        if (!Guid.TryParse(rawValue, out Guid authUserId))
        {
            return Task.FromResult<ParentUser?>(null);
        }

        return dbContext_.ParentUsers
            .FirstOrDefaultAsync(parentUser => parentUser.AuthUserId == authUserId, cancellationToken);
    }
}
