using Brush.Api.Contracts.Auth;
using Brush.Api.Development;
using Brush.Domain.Families;
using Brush.Domain.Parents;
using Brush.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brush.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly BrushDbContext dbContext_;

    public AuthController(BrushDbContext dbContext)
    {
        dbContext_ = dbContext;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterParentResponse>> Register(
        [FromBody] RegisterParentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.FamilyName))
        {
            return BadRequest(new
            {
                message = "FamilyName is required."
            });
        }

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        bool emailExists = await dbContext_.ParentUsers
            .AnyAsync(parentUser => parentUser.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "A parent user with this email already exists."
            });
        }

        Guid authUserId = request.AuthUserId ?? Guid.NewGuid();

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
}
