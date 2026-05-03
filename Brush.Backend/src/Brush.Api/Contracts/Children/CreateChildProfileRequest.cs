namespace Brush.Api.Contracts.Children;

public sealed record CreateChildProfileRequest(
    string Name,
    string PetName,
    int PictureId);
