using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DocMigrate.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestAppDbContext(options);
    }

    private class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // InMemory provider does not support NpgsqlTsVector computed columns
            modelBuilder.Entity<Page>().Ignore(p => p.SearchVector);
            modelBuilder.Entity<Space>().Ignore(s => s.SearchVector);

            // PageTranslation uses a shadow property for SearchVector
            modelBuilder.Entity<PageTranslation>(entity =>
            {
                entity.Ignore("SearchVector");
            });
        }
    }
}
