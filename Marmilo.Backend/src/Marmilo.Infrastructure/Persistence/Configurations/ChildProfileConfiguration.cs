using Marmilo.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marmilo.Infrastructure.Persistence.Configurations;

public sealed class ChildProfileConfiguration : IEntityTypeConfiguration<ChildProfile>
{
    public void Configure(EntityTypeBuilder<ChildProfile> builder)
    {
        builder.ToTable("child_profiles");

        builder.HasKey(childProfile => childProfile.Id);

        builder.Property(childProfile => childProfile.Id)
            .HasColumnName("id");

        builder.Property(childProfile => childProfile.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(childProfile => childProfile.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(childProfile => childProfile.PetName)
            .HasColumnName("pet_name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(childProfile => childProfile.PictureId)
            .HasColumnName("picture_id")
            .IsRequired();

        builder.Property(childProfile => childProfile.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(childProfile => childProfile.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(childProfile => childProfile.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(childProfile => childProfile.FamilyId);

        builder.HasOne(childProfile => childProfile.GameState)
            .WithOne(gameState => gameState.ChildProfile)
            .HasForeignKey<Marmilo.Domain.GameState.ChildGameState>(gameState => gameState.ChildProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
