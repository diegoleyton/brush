using Marmilo.Domain.GameState;

namespace Marmilo.Domain.Tests;

public sealed class ChildGameStateTests
{
    [Fact]
    public void Update_rejects_non_positive_brush_session_duration()
    {
        ChildGameState state = new(Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            state.Update(0, false, false, "{}", "{}", "{}"));
    }

    [Fact]
    public void Update_normalizes_blank_json_payloads_to_empty_objects()
    {
        ChildGameState state = new(Guid.NewGuid());

        state.Update(2, true, true, "", " ", null!);

        Assert.Equal("{}", state.PetStateJson);
        Assert.Equal("{}", state.RoomStateJson);
        Assert.Equal("{}", state.InventoryStateJson);
    }

    [Fact]
    public void SetCoinsBalance_rejects_negative_values()
    {
        ChildGameState state = new(Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => state.SetCoinsBalance(-1));
    }
}
