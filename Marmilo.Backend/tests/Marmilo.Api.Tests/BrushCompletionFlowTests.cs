using Marmilo.Api.Tests.Infrastructure;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests;

public sealed class BrushCompletionFlowTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public BrushCompletionFlowTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Brush_completion_marks_pending_reward_and_updates_pet_state()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "brush@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Brush Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        JsonObject completedState = await flow.CompleteBrushSessionAsync(childId);
        JsonObject refreshedState = await flow.GetGameStateAsync(childId);

        Assert.True(completedState["pendingReward"]?.GetValue<bool>());
        Assert.True(refreshedState["pendingReward"]?.GetValue<bool>());

        JsonObject petState = refreshedState["petState"]?.AsObject()
            ?? throw new InvalidOperationException("Expected petState object.");

        Assert.True(petState["lastBrushTime"]?.GetValue<long>() > 0);
    }

    [Fact]
    public async Task Brush_completion_fails_when_called_during_cooldown()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "brush-cooldown@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Brush Cooldown Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        await flow.CompleteBrushSessionAsync(childId);

        using HttpClient client = flow.CreateClient();
        HttpResponseMessage secondResponse = await client.PostAsync($"/children/{childId}/brush-completions", content: null);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, secondResponse.StatusCode);

        JsonObject error = await secondResponse.ReadJsonObjectAsync();
        Assert.Equal("Brush session is still on cooldown.", error["message"]?.GetValue<string>());
    }
}
