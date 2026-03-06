using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("secoes");

        builder.HasKey(e => e.Id).HasName("pk_secoes");
        builder.Property(e => e.Id).HasColumnName("secoesid");

        builder.Property(e => e.Title).HasColumnName("titulo").HasMaxLength(255).IsRequired();
        builder.Property(e => e.SortOrder).HasColumnName("ordem").HasDefaultValue(0);

        builder.Property(e => e.SpaceId).HasColumnName("espacoid");

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_secoes_desativadoem");

        builder.HasMany(e => e.Pages)
            .WithOne(p => p.Section)
            .HasForeignKey(p => p.SectionId)
            .HasConstraintName("fk_paginas_secaoid")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
