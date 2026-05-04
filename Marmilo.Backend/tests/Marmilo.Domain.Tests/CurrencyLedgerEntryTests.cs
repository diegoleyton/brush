using Marmilo.Domain.Currency;

namespace Marmilo.Domain.Tests;

public sealed class CurrencyLedgerEntryTests
{
    [Fact]
    public void Constructor_rejects_non_positive_amount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CurrencyLedgerEntry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                CurrencyLedgerEntryType.Grant,
                0,
                Guid.NewGuid(),
                null,
                "{}"));
    }

    [Fact]
    public void Constructor_normalizes_blank_metadata_to_empty_object()
    {
        CurrencyLedgerEntry entry = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CurrencyLedgerEntryType.Grant,
            5,
            Guid.NewGuid(),
            null,
            "");

        Assert.Equal("{}", entry.MetadataJson);
    }

    [Fact]
    public void Constructor_rejects_invalid_json_metadata()
    {
        Assert.Throws<ArgumentException>(() =>
            new CurrencyLedgerEntry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                CurrencyLedgerEntryType.Grant,
                5,
                Guid.NewGuid(),
                null,
                "{"));
    }
}
