using System.Text.Json;

namespace Marmilo.Api.Contracts.Children;

public sealed record ChildGameStateResponse(
    Guid ChildId,
    string Revision,
    int CoinsBalance,
    int BrushSessionDurationMinutes,
    int PendingRewardCount,
    bool Muted,
    JsonElement PetState,
    JsonElement RoomState,
    JsonElement InventoryState);
