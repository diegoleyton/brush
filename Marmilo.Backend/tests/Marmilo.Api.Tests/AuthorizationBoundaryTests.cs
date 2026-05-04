using Marmilo.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests;

public sealed class AuthorizationBoundaryTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public AuthorizationBoundaryTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Parent_from_another_family_cannot_read_foreign_child_profile()
    {
        await factory_.ResetDatabaseAsync();

        TestFlowDriver ownerFlow = new(factory_, new TestAuthSession(Guid.NewGuid(), "owner@marmilo.test"));
        await ownerFlow.RegisterParentAsync("Owner Family");
        JsonObject child = await ownerFlow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        TestFlowDriver outsiderFlow = new(factory_, new TestAuthSession(Guid.NewGuid(), "outsider@marmilo.test"));
        await outsiderFlow.RegisterParentAsync("Outsider Family");

        using HttpClient client = outsiderFlow.CreateClient();
        HttpResponseMessage response = await client.GetAsync($"/children/{childId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Redeem_with_insufficient_balance_returns_bad_request_and_keeps_balance()
    {
        await factory_.ResetDatabaseAsync();

        TestFlowDriver flow = new(factory_, new TestAuthSession(Guid.NewGuid(), "insufficient@marmilo.test"));
        await flow.RegisterParentAsync("Marmilo Balance Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        JsonObject marketItem = await flow.CreateMarketItemAsync("Ir a tomar helado", "Salida especial", 5);
        Guid marketItemId = marketItem["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected market item id.");

        using HttpClient client = flow.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/children/{childId}/redemptions",
            new
            {
                marketItemId
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonObject body = await response.ReadJsonObjectAsync();
        Assert.Equal("Not enough coins to redeem this item.", body["message"]?.GetValue<string>());

        JsonObject gameState = await flow.GetGameStateAsync(childId);
        Assert.Equal(0, gameState["coinsBalance"]?.GetValue<int>());

        JsonObject redemptions = await flow.GetRedemptionsAsync(childId);
        JsonArray redemptionItems = redemptions["items"]?.AsArray()
            ?? throw new InvalidOperationException("Expected redemptions array.");
        Assert.Empty(redemptionItems);

        JsonObject ledger = await flow.GetLedgerAsync(childId);
        JsonArray ledgerItems = ledger["items"]?.AsArray()
            ?? throw new InvalidOperationException("Expected ledger items.");
        Assert.Empty(ledgerItems);
    }
}
