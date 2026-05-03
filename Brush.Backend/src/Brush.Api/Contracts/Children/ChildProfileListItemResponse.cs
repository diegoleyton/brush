namespace Brush.Api.Contracts.Children;

public sealed record ChildProfileListItemResponse(
    Guid Id,
    string Name,
    string PetName,
    int PictureId,
    bool IsActive);
