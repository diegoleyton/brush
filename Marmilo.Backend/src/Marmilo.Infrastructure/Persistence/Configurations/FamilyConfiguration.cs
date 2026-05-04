using Marmilo.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marmilo.Infrastructure.Persistence.Configurations;

public sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("families");

        builder.HasKey(family => family.Id);

        builder.Property(family => family.Id)
            .HasColumnName("id");

        builder.Property(family => family.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(family => family.CreatedByParentUserId)
            .HasColumnName("created_by_parent_user_id")
            .IsRequired();

        builder.Property(family => family.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(family => family.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasMany(family => family.ParentMemberships)
            .WithOne(membership => membership.Family)
            .HasForeignKey(membership => membership.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(family => family.ChildProfiles)
            .WithOne(childProfile => childProfile.Family)
            .HasForeignKey(childProfile => childProfile.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
