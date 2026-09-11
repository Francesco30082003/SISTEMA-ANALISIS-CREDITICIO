using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence;

public sealed class MapanDbContext(DbContextOptions<MapanDbContext> options)
    : DbContext(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    private void GuardHistory()
    {
        ChangeTracker.DetectChanges();
        if(ChangeTracker.Entries().Any(e=>e.Entity is SnapshotFinanciero or VectorCaracteristicas&&e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Los snapshots y vectores históricos son inmutables.");
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess){GuardHistory();return base.SaveChanges(acceptAllChangesOnSuccess);}
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,CancellationToken cancellationToken=default){GuardHistory();return base.SaveChangesAsync(acceptAllChangesOnSuccess,cancellationToken);}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MapanDbContext).Assembly);
        // Explicit booleans must survive INSERT even when PostgreSQL has a
        // different default (e.g. a non-recurring income source).
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties())
                     .Where(property => property.ClrType == typeof(bool) && property.GetDefaultValueSql() is not null))
            property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
    }
}
