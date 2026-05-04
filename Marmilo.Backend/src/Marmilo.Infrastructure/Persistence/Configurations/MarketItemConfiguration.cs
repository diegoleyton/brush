using Marmilo.Domain.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marmilo.Infrastructure.Persistence.Configurations;

public sealed class MarketItemConfiguration : IEntityTypeConfiguration<MarketItem>
{
    public void Configure(EntityTypeBuilder<MarketItem> builder)
    {
        builder.ToTable("market_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(item => item.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(item => item.Price)
            .HasColumnName("price")
            .IsRequired();

        builder.Property(item => item.ItemType)
            .HasColumnName("item_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(item => item.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(item => item.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(item => item.FamilyId);

        builder.HasOne(item => item.Family)
            .WithMany()
            .HasForeignKey(item => item.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
