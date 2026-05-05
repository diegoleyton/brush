using System.Net;
using System.Text.Json.Nodes;

using Marmilo.Api.Tests.Infrastructure;

namespace Marmilo.Api.Tests;

public sealed class InGameMarketPurchaseFlowTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public InGameMarketPurchaseFlowTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Market_purchase_updates_balance_and_inventory()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "market@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Market Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        await flow.GrantRewardAsync(childId, (await flow.CreateRewardRuleAsync("Ordenar", "Ordenar sus juguetes", 20))["id"]!.GetValue<Guid>());

        HttpResponseMessage purchaseResponse = await flow.PurchaseInGameMarketItemAsync(childId, itemType: 5, itemId: 1);
        Assert.Equal(HttpStatusCode.NoContent, purchaseResponse.StatusCode);

        JsonObject gameState = await flow.GetGameStateAsync(childId);
        Assert.Equal(15, gameState["coinsBalance"]?.GetValue<int>());

        JsonObject inventoryState = gameState["inventoryState"]?.AsObject()
            ?? throw new InvalidOperationException("Expected inventoryState object.");
        JsonArray foodItems = inventoryState["foodSerialized_"]?.AsArray()
            ?? throw new InvalidOperationException("Expected foodSerialized_ array.");
        JsonObject purchasedFood = foodItems
            .Select(node => node?.AsObject())
            .Single(item => item?["ItemId"]?.GetValue<int>() == 1)
            ?? throw new InvalidOperationException("Expected purchased food item.");

        Assert.Equal(3, purchasedFood["Amount"]?.GetValue<int>());
    }

    [Fact]
    public async Task Market_purchase_fails_when_not_enough_coins()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "market-empty@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Market Empty Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        HttpResponseMessage purchaseResponse = await flow.PurchaseInGameMarketItemAsync(childId, itemType: 5, itemId: 1);

        Assert.Equal(HttpStatusCode.BadRequest, purchaseResponse.StatusCode);

        JsonObject error = await purchaseResponse.ReadJsonObjectAsync();
        Assert.Equal("Not enough coins to buy this item.", error["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task Market_purchase_fails_when_item_is_already_owned()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "market-owned@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Market Owned Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        await flow.GrantRewardAsync(childId, (await flow.CreateRewardRuleAsync("Ordenar", "Ordenar sus juguetes", 50))["id"]!.GetValue<Guid>());
        HttpResponseMessage firstPurchase = await flow.PurchaseInGameMarketItemAsync(childId, itemType: 4, itemId: 1);
        Assert.Equal(HttpStatusCode.NoContent, firstPurchase.StatusCode);

        HttpResponseMessage secondPurchase = await flow.PurchaseInGameMarketItemAsync(childId, itemType: 4, itemId: 1);
        Assert.Equal(HttpStatusCode.BadRequest, secondPurchase.StatusCode);

        JsonObject error = await secondPurchase.ReadJsonObjectAsync();
        Assert.Equal("Item is already owned.", error["message"]?.GetValue<string>());
    }
}
