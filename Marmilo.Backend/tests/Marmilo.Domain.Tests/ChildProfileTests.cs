using Marmilo.Domain.Families;

namespace Marmilo.Domain.Tests;

public sealed class ChildProfileTests
{
    [Fact]
    public void Constructor_trims_name_and_pet_name()
    {
        ChildProfile child = new(Guid.NewGuid(), "  Sofia  ", "  Nube  ", 1);

        Assert.Equal("Sofia", child.Name);
        Assert.Equal("Nube", child.PetName);
    }

    [Fact]
    public void SetPicture_rejects_non_positive_values()
    {
        ChildProfile child = new(Guid.NewGuid(), "Sofia", "Nube", 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => child.SetPicture(0));
    }

    [Fact]
    public void Rename_rejects_blank_name()
    {
        ChildProfile child = new(Guid.NewGuid(), "Sofia", "Nube", 1);

        Assert.Throws<ArgumentException>(() => child.Rename(" "));
    }
}
