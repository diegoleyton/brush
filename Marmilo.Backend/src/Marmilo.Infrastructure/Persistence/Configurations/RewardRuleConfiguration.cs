using Marmilo.Domain.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marmilo.Infrastructure.Persistence.Configurations;

public sealed class RewardRuleConfiguration : IEntityTypeConfiguration<RewardRule>
{
    public void Configure(EntityTypeBuilder<RewardRule> builder)
    {
        builder.ToTable("reward_rules");

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Id)
            .HasColumnName("id");

        builder.Property(rule => rule.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(rule => rule.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(rule => rule.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(rule => rule.CurrencyAmount)
            .HasColumnName("currency_amount")
            .IsRequired();

        builder.Property(rule => rule.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(rule => rule.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(rule => rule.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(rule => rule.FamilyId);

        builder.HasOne(rule => rule.Family)
            .WithMany()
            .HasForeignKey(rule => rule.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
