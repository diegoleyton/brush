using Marmilo.Domain.GameState;

namespace Marmilo.Domain.Tests;

public sealed class ChildGameStateTests
{
    [Fact]
    public void Update_rejects_non_positive_brush_session_duration()
    {
        ChildGameState state = new(Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            state.Update(0, 0, false, "{}", "{}", "{}"));
    }

    [Fact]
    public void Update_normalizes_blank_json_payloads_to_empty_objects()
    {
        ChildGameState state = new(Guid.NewGuid());

        state.Update(2, 1, true, "", " ", null!);

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

    [Fact]
    public void TryCompleteBrushSession_sets_last_brush_time_and_pending_reward_when_not_on_cooldown()
    {
        ChildGameState state = new(Guid.NewGuid(), "Nube");
        state.Update(2, pendingRewardCount: 0, muted: false, state.PetStateJson, state.RoomStateJson, state.InventoryStateJson);

        long nowUnixSeconds = 1_700_000_000;

        bool completed = state.TryCompleteBrushSession("Nube", nowUnixSeconds);

        Assert.True(completed);
        Assert.Equal(1, state.PendingRewardCount);
        Assert.Equal(nowUnixSeconds, ChildGameStateDefaults.GetLastBrushTime(state.PetStateJson, "Nube"));
    }

    [Fact]
    public void TryCompleteBrushSession_rejects_when_brush_is_on_cooldown()
    {
        ChildGameState state = new(Guid.NewGuid(), "Nube");
        long firstBrushUnixSeconds = 1_700_000_000;
        long blockedRetryUnixSeconds = firstBrushUnixSeconds + ChildGameState.BrushCooldownSeconds - 1;

        bool firstCompletion = state.TryCompleteBrushSession("Nube", firstBrushUnixSeconds);
        bool secondCompletion = state.TryCompleteBrushSession("Nube", blockedRetryUnixSeconds);

        Assert.True(firstCompletion);
        Assert.False(secondCompletion);
        Assert.Equal(firstBrushUnixSeconds, ChildGameStateDefaults.GetLastBrushTime(state.PetStateJson, "Nube"));
    }
}
