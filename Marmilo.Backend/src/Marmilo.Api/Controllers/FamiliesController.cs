using Marmilo.Api.Auth;
using Marmilo.Api.Contracts.Families;
using Marmilo.Api.Development;
using Marmilo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marmilo.Api.Controllers;

[ApiController]
[Route("families")]
public sealed class FamiliesController : ControllerBase
{
    private readonly MarmiloDbContext dbContext_;
    private readonly ICurrentPrincipalContextAccessor currentPrincipalContextAccessor_;

    public FamiliesController(
        MarmiloDbContext dbContext,
        ICurrentPrincipalContextAccessor currentPrincipalContextAccessor)
    {
        dbContext_ = dbContext;
        currentPrincipalContextAccessor_ = currentPrincipalContextAccessor;
    }

    [HttpGet("current")]
    public async Task<ActionResult<CurrentFamilyResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext?.ParentUser == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header."
            });
        }

        var family = await dbContext_.Families
            .Where(family => family.ParentMemberships.Any(membership => membership.ParentUserId == principalContext.ParentUser.Id))
            .Include(currentFamily => currentFamily.ParentMemberships)
                .ThenInclude(membership => membership.ParentUser)
            .FirstOrDefaultAsync(cancellationToken);

        if (family == null)
        {
            return NotFound(new
            {
                message = "No family was found for the current parent."
            });
        }

        CurrentFamilyResponse response = new(
            family.Id,
            family.Name,
            family.ParentMemberships
                .OrderBy(membership => membership.CreatedAt)
                .Select(membership => new CurrentFamilyParentResponse(
                    membership.ParentUser.Id,
                    membership.ParentUser.AuthUserId,
                    membership.ParentUser.Email,
                    membership.Role.ToString().ToLowerInvariant()))
                .ToList());

        return Ok(response);
    }
}
