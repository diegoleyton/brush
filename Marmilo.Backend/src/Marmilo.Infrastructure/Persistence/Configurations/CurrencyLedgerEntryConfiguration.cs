using Marmilo.Domain.Currency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marmilo.Infrastructure.Persistence.Configurations;

public sealed class CurrencyLedgerEntryConfiguration : IEntityTypeConfiguration<CurrencyLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CurrencyLedgerEntry> builder)
    {
        builder.ToTable("currency_ledger");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id");

        builder.Property(entry => entry.FamilyId)
            .HasColumnName("family_id")
            .IsRequired();

        builder.Property(entry => entry.ChildProfileId)
            .HasColumnName("child_profile_id")
            .IsRequired();

        builder.Property(entry => entry.EntryType)
            .HasColumnName("entry_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entry => entry.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(entry => entry.CreatedByParentUserId)
            .HasColumnName("created_by_parent_user_id");

        builder.Property(entry => entry.RewardRuleId)
            .HasColumnName("reward_rule_id");

        builder.Property(entry => entry.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(entry => entry.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(entry => entry.ChildProfileId);
        builder.HasIndex(entry => entry.FamilyId);

        builder.HasOne(entry => entry.Family)
            .WithMany()
            .HasForeignKey(entry => entry.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entry => entry.ChildProfile)
            .WithMany()
            .HasForeignKey(entry => entry.ChildProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entry => entry.CreatedByParentUser)
            .WithMany()
            .HasForeignKey(entry => entry.CreatedByParentUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(entry => entry.RewardRule)
            .WithMany()
            .HasForeignKey(entry => entry.RewardRuleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
