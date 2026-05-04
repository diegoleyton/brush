using Marmilo.Domain.Redemptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marmilo.Infrastructure.Persistence.Configurations;

public sealed class RedemptionConfiguration : IEntityTypeConfiguration<Redemption>
{
    public void Configure(EntityTypeBuilder<Redemption> builder)
    {
        builder.ToTable("redemptions");

        builder.HasKey(redemption => redemption.Id);

        builder.Property(redemption => redemption.Id)
            .HasColumnName("id");

        builder.Property(redemption => redemption.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(redemption => redemption.ChildProfileId)
            .HasColumnName("child_profile_id")
            .IsRequired();

        builder.Property(redemption => redemption.MarketItemId)
            .HasColumnName("market_item_id")
            .IsRequired();

        builder.Property(redemption => redemption.Cost)
            .HasColumnName("cost")
            .IsRequired();

        builder.Property(redemption => redemption.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(redemption => redemption.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(redemption => redemption.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(redemption => redemption.ResolvedByParentUserId)
            .HasColumnName("resolved_by_parent_user_id");

        builder.HasIndex(redemption => redemption.ChildProfileId);
        builder.HasIndex(redemption => redemption.FamilyId);
        builder.HasIndex(redemption => redemption.MarketItemId);

        builder.HasOne(redemption => redemption.Family)
            .WithMany()
            .HasForeignKey(redemption => redemption.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(redemption => redemption.ChildProfile)
            .WithMany()
            .HasForeignKey(redemption => redemption.ChildProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(redemption => redemption.MarketItem)
            .WithMany()
            .HasForeignKey(redemption => redemption.MarketItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(redemption => redemption.ResolvedByParentUser)
            .WithMany()
            .HasForeignKey(redemption => redemption.ResolvedByParentUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
