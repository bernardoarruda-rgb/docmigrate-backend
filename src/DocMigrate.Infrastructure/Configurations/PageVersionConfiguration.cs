using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class PageVersionConfiguration : IEntityTypeConfiguration<PageVersion>
{
    public void Configure(EntityTypeBuilder<PageVersion> builder)
    {
        builder.ToTable("paginas_versoes");

        builder.HasKey(e => e.Id).HasName("pk_paginas_versoes");
        builder.Property(e => e.Id).HasColumnName("paginasversoesid");

        builder.Property(e => e.VersionNumber).HasColumnName("versaonumero").IsRequired();
        builder.Property(e => e.Content).HasColumnName("conteudo").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.PlainText).HasColumnName("textoplano");
        builder.Property(e => e.ChangeDescription).HasColumnName("descricaomudanca").HasMaxLength(500);

        builder.Property(e => e.PageId).HasColumnName("paginaid");
        builder.HasOne(e => e.Page)
            .WithMany()
            .HasForeignKey(e => e.PageId)
            .HasConstraintName("fk_paginas_versoes_paginaid")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.CreatedByUserId).HasColumnName("criadoporusuarioid");
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .HasConstraintName("fk_paginas_versoes_criadoporusuarioid")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.PageId).HasDatabaseName("idx_paginas_versoes_paginaid");

        builder.HasIndex(e => new { e.PageId, e.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_paginas_versoes_paginaid_versaonumero");
    }
}
