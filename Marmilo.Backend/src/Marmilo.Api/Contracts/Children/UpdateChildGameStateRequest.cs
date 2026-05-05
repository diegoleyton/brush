using System.Text.Json;

namespace Marmilo.Api.Contracts.Children;

public sealed record UpdateChildGameStateRequest(
    string? BaseRevision,
    int BrushSessionDurationMinutes,
    int PendingRewardCount,
    bool Muted,
    JsonElement PetState,
    JsonElement RoomState,
    JsonElement InventoryState);
