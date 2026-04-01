using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<PageVersion> PageVersions => Set<PageVersion>();
    public DbSet<PageFavorite> PageFavorites => Set<PageFavorite>();
    public DbSet<PageVisit> PageVisits => Set<PageVisit>();
    public DbSet<PageComment> PageComments => Set<PageComment>();
    public DbSet<PageTranslation> PageTranslations => Set<PageTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Domain.Common.BaseEntity entity)
                entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
