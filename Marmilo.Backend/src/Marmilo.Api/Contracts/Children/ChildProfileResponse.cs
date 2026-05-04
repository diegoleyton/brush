namespace Marmilo.Api.Contracts.Children;

public sealed record ChildProfileResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    string PetName,
    int PictureId,
    bool IsActive);
