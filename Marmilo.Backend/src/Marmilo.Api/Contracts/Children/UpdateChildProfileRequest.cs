namespace Marmilo.Api.Contracts.Children;

public sealed record UpdateChildProfileRequest(
    string Name,
    string PetName,
    int PictureId,
    bool IsActive);
