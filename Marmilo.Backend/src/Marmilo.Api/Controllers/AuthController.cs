using Marmilo.Api.Contracts.Auth;
using Marmilo.Api.Auth;
using Marmilo.Api.Development;
using Marmilo.Domain.Families;
using Marmilo.Domain.Parents;
using Marmilo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marmilo.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly MarmiloDbContext dbContext_;
    private readonly ICurrentPrincipalContextAccessor currentPrincipalContextAccessor_;

    public AuthController(
        MarmiloDbContext dbContext,
        ICurrentPrincipalContextAccessor currentPrincipalContextAccessor)
    {
        dbContext_ = dbContext;
        currentPrincipalContextAccessor_ = currentPrincipalContextAccessor;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterParentResponse>> Register(
        [FromBody] RegisterParentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FamilyName))
        {
            return BadRequest(new
            {
                message = "FamilyName is required."
            });
        }

        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        string? normalizedEmail =
            principalContext?.Email
            ?? NormalizeEmail(request.Email);

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest(new
            {
                message = "Email is required. Provide a valid Supabase bearer token or an Email value for development bootstrap."
            });
        }

        Guid authUserId =
            principalContext?.AuthUserId
            ?? request.AuthUserId
            ?? Guid.NewGuid();

        if (principalContext?.ParentUser != null)
        {
            return Conflict(new
            {
                message = "A parent user already exists for the current authenticated identity."
            });
        }

        bool emailExists = await dbContext_.ParentUsers
            .AnyAsync(parentUser => parentUser.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "A parent user with this email already exists."
            });
        }

        ParentUser parentUser = new(authUserId, normalizedEmail);
        Family family = new(request.FamilyName, parentUser.Id);
        FamilyParentMembership membership = new(family.Id, parentUser.Id, FamilyParentRole.Owner);

        dbContext_.ParentUsers.Add(parentUser);
        dbContext_.Families.Add(family);
        dbContext_.FamilyParents.Add(membership);

        await dbContext_.SaveChangesAsync(cancellationToken);

        RegisterParentResponse response = new(
            new ParentSummaryResponse(parentUser.Id, parentUser.AuthUserId, parentUser.Email),
            new FamilySummaryResponse(family.Id, family.Name),
            DevelopmentAuthDefaults.ParentAuthUserIdHeaderName,
            parentUser.AuthUserId.ToString());

        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult<object>> Me(CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext == null)
        {
            return Unauthorized(new
            {
                message = "No authenticated principal was found."
            });
        }

        return Ok(new
        {
            authUserId = principalContext.AuthUserId,
            email = principalContext.Email,
            isDevelopmentFallback = principalContext.IsDevelopmentFallback,
            parentUserId = principalContext.ParentUser?.Id
        });
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
