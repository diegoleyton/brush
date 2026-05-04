using Marmilo.Api.Tests.Infrastructure;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests;

public sealed class ChildProfileManagementTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public ChildProfileManagementTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Update_child_profile_persists_renamed_pet_picture_and_active_state()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "children@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Children Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        JsonObject updated = await flow.UpdateChildAsync(childId, "Sofia Actualizada", "Luna", 2, false);
        JsonObject fetched = await flow.GetChildAsync(childId);

        Assert.Equal("Sofia Actualizada", updated["name"]?.GetValue<string>());
        Assert.Equal("Luna", updated["petName"]?.GetValue<string>());
        Assert.Equal(2, updated["pictureId"]?.GetValue<int>());
        Assert.False(updated["isActive"]?.GetValue<bool>());

        Assert.Equal("Sofia Actualizada", fetched["name"]?.GetValue<string>());
        Assert.Equal("Luna", fetched["petName"]?.GetValue<string>());
        Assert.Equal(2, fetched["pictureId"]?.GetValue<int>());
        Assert.False(fetched["isActive"]?.GetValue<bool>());
    }
}
