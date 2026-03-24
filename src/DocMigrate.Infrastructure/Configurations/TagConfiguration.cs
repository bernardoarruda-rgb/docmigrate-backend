using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(e => e.Id).HasName("pk_tags");
        builder.Property(e => e.Id).HasColumnName("tagsid");

        builder.Property(e => e.Name).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Color).HasColumnName("cor").HasMaxLength(7);

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.Name).IsUnique().HasDatabaseName("uq_tags_nome").HasFilter("desativadoem IS NULL");
        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_tags_desativadoem");

        // Many-to-many with Pages
        builder.HasMany(e => e.Pages)
            .WithMany(p => p.Tags)
            .UsingEntity(
                "paginas_tags",
                l => l.HasOne(typeof(Page)).WithMany().HasForeignKey("paginaid").HasConstraintName("fk_paginas_tags_paginaid"),
                r => r.HasOne(typeof(Tag)).WithMany().HasForeignKey("tagid").HasConstraintName("fk_paginas_tags_tagid"),
                j =>
                {
                    j.HasKey("paginaid", "tagid").HasName("pk_paginas_tags");
                    j.ToTable("paginas_tags");
                });

        // Many-to-many with Spaces
        builder.HasMany(e => e.Spaces)
            .WithMany(s => s.Tags)
            .UsingEntity(
                "espacos_tags",
                l => l.HasOne(typeof(Space)).WithMany().HasForeignKey("espacoid").HasConstraintName("fk_espacos_tags_espacoid"),
                r => r.HasOne(typeof(Tag)).WithMany().HasForeignKey("tagid").HasConstraintName("fk_espacos_tags_tagid"),
                j =>
                {
                    j.HasKey("espacoid", "tagid").HasName("pk_espacos_tags");
                    j.ToTable("espacos_tags");
                });
    }
}
