using System.Net;
using System.Text.Json.Nodes;

using Marmilo.Api.Tests.Infrastructure;

namespace Marmilo.Api.Tests;

public sealed class ChildGameStateConflictTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public ChildGameStateConflictTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Update_game_state_returns_conflict_when_base_revision_is_stale()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "conflict@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Conflict Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        JsonObject originalState = await flow.GetGameStateAsync(childId);
        string originalRevision = originalState["revision"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Expected revision.");

        HttpResponseMessage firstUpdate = await flow.UpdateGameStateAsync(
            childId,
            baseRevision: originalRevision,
            brushSessionDurationMinutes: 3,
            pendingRewardCount: 1,
            muted: false,
            petState: originalState["petState"]!.DeepClone(),
            roomState: originalState["roomState"]!.DeepClone(),
            inventoryState: originalState["inventoryState"]!.DeepClone());
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        HttpResponseMessage staleUpdate = await flow.UpdateGameStateAsync(
            childId,
            baseRevision: originalRevision,
            brushSessionDurationMinutes: 4,
            pendingRewardCount: 0,
            muted: true,
            petState: originalState["petState"]!.DeepClone(),
            roomState: originalState["roomState"]!.DeepClone(),
            inventoryState: originalState["inventoryState"]!.DeepClone());

        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);

        JsonObject conflict = await staleUpdate.ReadJsonObjectAsync();
        Assert.Equal("Child game state changed on the server.", conflict["message"]?.GetValue<string>());
        Assert.NotEqual(originalRevision, conflict["revision"]?.GetValue<string>());
    }
}
