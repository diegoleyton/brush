using Brush.Domain.Parents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Brush.Infrastructure.Persistence.Configurations;

public sealed class ParentUserConfiguration : IEntityTypeConfiguration<ParentUser>
{
    public void Configure(EntityTypeBuilder<ParentUser> builder)
    {
        builder.ToTable("parent_users");

        builder.HasKey(parentUser => parentUser.Id);

        builder.Property(parentUser => parentUser.Id)
            .HasColumnName("id");

        builder.Property(parentUser => parentUser.AuthUserId)
            .HasColumnName("auth_user_id")
            .IsRequired();

        builder.Property(parentUser => parentUser.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(parentUser => parentUser.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(parentUser => parentUser.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(parentUser => parentUser.AuthUserId).IsUnique();
        builder.HasIndex(parentUser => parentUser.Email).IsUnique();
    }
}
