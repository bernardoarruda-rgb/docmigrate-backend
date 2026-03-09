using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("paginas");

        builder.HasKey(e => e.Id).HasName("pk_paginas");
        builder.Property(e => e.Id).HasColumnName("paginasid");

        builder.Property(e => e.Title).HasColumnName("titulo").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Description).HasColumnName("descricao").HasMaxLength(500);
        builder.Property(e => e.Content).HasColumnName("conteudo").HasColumnType("jsonb");
        builder.Property(e => e.SortOrder).HasColumnName("ordem").HasDefaultValue(0);

        builder.Property(e => e.SpaceId).HasColumnName("espacoid");

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_paginas_desativadoem");
    }
}
