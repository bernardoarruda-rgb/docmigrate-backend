using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.ToTable("espacos");

        builder.HasKey(e => e.Id).HasName("pk_espacos");
        builder.Property(e => e.Id).HasColumnName("espacosid");

        builder.Property(e => e.Title).HasColumnName("titulo").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Description).HasColumnName("descricao").HasMaxLength(500);

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_espacos_desativadoem");

        builder.HasMany(e => e.Pages)
            .WithOne(p => p.Space)
            .HasForeignKey(p => p.SpaceId)
            .HasConstraintName("fk_paginas_espacoid")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
