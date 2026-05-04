using Marmilo.Domain.Currency;
using Marmilo.Domain.Families;
using Marmilo.Domain.GameState;
using Marmilo.Domain.Market;
using Marmilo.Domain.Parents;
using Marmilo.Domain.Redemptions;
using Marmilo.Domain.Rewards;
using Microsoft.EntityFrameworkCore;

namespace Marmilo.Infrastructure.Persistence;

public sealed class MarmiloDbContext : DbContext
{
    public MarmiloDbContext(DbContextOptions<MarmiloDbContext> options)
        : base(options)
    {
    }

    public DbSet<ParentUser> ParentUsers => Set<ParentUser>();

    public DbSet<Family> Families => Set<Family>();

    public DbSet<FamilyParentMembership> FamilyParents => Set<FamilyParentMembership>();

    public DbSet<ChildProfile> ChildProfiles => Set<ChildProfile>();

    public DbSet<ChildGameState> ChildGameStates => Set<ChildGameState>();

    public DbSet<CurrencyLedgerEntry> CurrencyLedgerEntries => Set<CurrencyLedgerEntry>();

    public DbSet<RewardRule> RewardRules => Set<RewardRule>();

    public DbSet<MarketItem> MarketItems => Set<MarketItem>();

    public DbSet<Redemption> Redemptions => Set<Redemption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InfrastructureMarker).Assembly);
    }
}
