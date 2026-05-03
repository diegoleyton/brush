using Brush.Domain.Families;
using Brush.Domain.Parents;
using Microsoft.EntityFrameworkCore;

namespace Brush.Infrastructure.Persistence;

public sealed class BrushDbContext : DbContext
{
    public BrushDbContext(DbContextOptions<BrushDbContext> options)
        : base(options)
    {
    }

    public DbSet<ParentUser> ParentUsers => Set<ParentUser>();

    public DbSet<Family> Families => Set<Family>();

    public DbSet<FamilyParentMembership> FamilyParents => Set<FamilyParentMembership>();

    public DbSet<ChildProfile> ChildProfiles => Set<ChildProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InfrastructureMarker).Assembly);
    }
}
