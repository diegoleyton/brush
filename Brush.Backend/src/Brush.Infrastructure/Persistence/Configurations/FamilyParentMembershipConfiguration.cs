using Brush.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brush.Infrastructure.Persistence.Configurations;

public sealed class FamilyParentMembershipConfiguration : IEntityTypeConfiguration<FamilyParentMembership>
{
    public void Configure(EntityTypeBuilder<FamilyParentMembership> builder)
    {
        builder.ToTable("family_parents");

        builder.HasKey(membership => new { membership.FamilyId, membership.ParentUserId });

        builder.Property(membership => membership.FamilyId)
            .HasColumnName("family_id");

        builder.Property(membership => membership.ParentUserId)
            .HasColumnName("parent_user_id");

        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(membership => membership.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(membership => membership.ParentUser)
            .WithMany(parentUser => parentUser.FamilyMemberships)
            .HasForeignKey(membership => membership.ParentUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
