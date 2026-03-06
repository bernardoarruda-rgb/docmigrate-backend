using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("sections");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.Property(e => e.SpaceId).HasColumnName("space_id");

        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.DeletedAt).HasFilter("deleted_at IS NULL");

        builder.HasMany(e => e.Pages)
            .WithOne(p => p.Section)
            .HasForeignKey(p => p.SectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
