using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.ToTable("spaces");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.DeletedAt).HasFilter("deleted_at IS NULL");

        builder.HasMany(e => e.Sections)
            .WithOne(s => s.Space)
            .HasForeignKey(s => s.SpaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
