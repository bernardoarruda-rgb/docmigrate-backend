using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class PageFavoriteConfiguration : IEntityTypeConfiguration<PageFavorite>
{
    public void Configure(EntityTypeBuilder<PageFavorite> builder)
    {
        builder.ToTable("paginas_favoritas");

        builder.HasKey(e => e.Id).HasName("pk_paginas_favoritas");
        builder.Property(e => e.Id).HasColumnName("paginasfavoritasid");

        builder.Property(e => e.UserId).HasColumnName("usuarioid");
        builder.Property(e => e.PageId).HasColumnName("paginaid");
        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("fk_paginas_favoritas_usuarioid")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Page)
            .WithMany()
            .HasForeignKey(e => e.PageId)
            .HasConstraintName("fk_paginas_favoritas_paginaid")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.PageId })
            .IsUnique()
            .HasDatabaseName("uq_paginas_favoritas_usuarioid_paginaid");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("idx_paginas_favoritas_usuarioid");
    }
}
