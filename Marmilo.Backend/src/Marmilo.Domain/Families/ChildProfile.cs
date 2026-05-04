using Marmilo.Domain.Common;
using Marmilo.Domain.GameState;

namespace Marmilo.Domain.Families;

public sealed class ChildProfile : Entity
{
    private ChildProfile()
    {
    }

    public ChildProfile(Guid familyId, string name, string petName, int pictureId)
    {
        FamilyId = familyId;
        Rename(name);
        RenamePet(petName);
        SetPicture(pictureId);
    }

    public Guid FamilyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string PetName { get; private set; } = string.Empty;

    public int PictureId { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Family Family { get; private set; } = null!;

    public ChildGameState GameState { get; private set; } = null!;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Child profile name is required.", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RenamePet(string petName)
    {
        if (string.IsNullOrWhiteSpace(petName))
        {
            throw new ArgumentException("Pet name is required.", nameof(petName));
        }

        PetName = petName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPicture(int pictureId)
    {
        if (pictureId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pictureId), "Picture id must be positive.");
        }

        PictureId = pictureId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
