using Marmilo.Api.Auth;
using Marmilo.Api.Contracts.Market;
using Marmilo.Api.Development;
using Marmilo.Domain.Market;
using Marmilo.Domain.Parents;
using Marmilo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Marmilo.Api.Controllers;

[ApiController]
[Route("market-items")]
public sealed class MarketItemsController : ControllerBase
{
    private readonly MarmiloDbContext dbContext_;
    private readonly ICurrentPrincipalContextAccessor currentPrincipalContextAccessor_;

    public MarketItemsController(
        MarmiloDbContext dbContext,
        ICurrentPrincipalContextAccessor currentPrincipalContextAccessor)
    {
        dbContext_ = dbContext;
        currentPrincipalContextAccessor_ = currentPrincipalContextAccessor;
    }

    [HttpGet]
    public async Task<ActionResult<MarketItemListResponse>> List(CancellationToken cancellationToken)
    {
        Guid? familyId = await ResolveCurrentFamilyIdAsync(cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var items = await dbContext_.MarketItems
            .Where(item => item.FamilyId == familyId.Value)
            .OrderBy(item => item.CreatedAt)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);

        return Ok(new MarketItemListResponse(items));
    }

    [HttpPost]
    public async Task<ActionResult<MarketItemResponse>> Create(
        [FromBody] CreateMarketItemRequest request,
        CancellationToken cancellationToken)
    {
        Guid? familyId = await ResolveCurrentFamilyIdAsync(cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        try
        {
            MarketItem item = new(
                familyId.Value,
                request.Title,
                request.Description,
                request.Price,
                ParseItemType(request.ItemType),
                SerializeJson(request.Payload));

            dbContext_.MarketItems.Add(item);
            await dbContext_.SaveChangesAsync(cancellationToken);
            return Created($"/market-items/{item.Id}", ToResponse(item));
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new { message = argumentException.Message });
        }
    }

    [HttpPatch("{marketItemId:guid}")]
    public async Task<ActionResult<MarketItemResponse>> Update(
        Guid marketItemId,
        [FromBody] UpdateMarketItemRequest request,
        CancellationToken cancellationToken)
    {
        Guid? familyId = await ResolveCurrentFamilyIdAsync(cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var item = await dbContext_.MarketItems
            .FirstOrDefaultAsync(item => item.FamilyId == familyId.Value && item.Id == marketItemId, cancellationToken);

        if (item == null)
        {
            return NotFound(new { message = "Market item was not found." });
        }

        try
        {
            item.Update(
                request.Title,
                request.Description,
                request.Price,
                ParseItemType(request.ItemType),
                SerializeJson(request.Payload),
                request.IsActive);

            await dbContext_.SaveChangesAsync(cancellationToken);
            return Ok(ToResponse(item));
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new { message = argumentException.Message });
        }
    }

    private async Task<Guid?> ResolveCurrentFamilyIdAsync(CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext?.ParentUser == null)
        {
            return null;
        }

        return await dbContext_.FamilyParents
            .Where(membership => membership.ParentUserId == principalContext.ParentUser.Id)
            .Select(membership => (Guid?)membership.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static MarketItemType ParseItemType(string rawItemType)
    {
        if (string.IsNullOrWhiteSpace(rawItemType))
        {
            throw new ArgumentException("ItemType is required.", nameof(rawItemType));
        }

        return rawItemType.Trim().ToLowerInvariant() switch
        {
            "realworldreward" => MarketItemType.RealWorldReward,
            "real_world_reward" => MarketItemType.RealWorldReward,
            "gameitem" => MarketItemType.GameItem,
            "game_item" => MarketItemType.GameItem,
            _ => throw new ArgumentException("Unsupported market item type.", nameof(rawItemType))
        };
    }

    private static string SerializeJson(JsonElement? payload) => payload?.GetRawText() ?? "{}";

    private static JsonElement ParseJson(string json)
    {
        using JsonDocument document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.Clone();
    }

    private static MarketItemResponse ToResponse(MarketItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.Price,
        item.ItemType.ToString(),
        ParseJson(item.PayloadJson),
        item.IsActive);
}
