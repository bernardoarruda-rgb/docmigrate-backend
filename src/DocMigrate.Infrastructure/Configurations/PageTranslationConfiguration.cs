using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class PageTranslationConfiguration : IEntityTypeConfiguration<PageTranslation>
{
    public void Configure(EntityTypeBuilder<PageTranslation> builder)
    {
        builder.ToTable("paginas_traducoes");

        builder.HasKey(e => e.Id)
            .HasName("pk_paginas_traducoes");
        builder.Property(e => e.Id)
            .HasColumnName("paginastraducoesid");

        builder.Property(e => e.PageId)
            .HasColumnName("paginaid")
            .IsRequired();

        builder.Property(e => e.Language)
            .HasColumnName("idioma")
            .HasMaxLength(5)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasColumnName("titulo")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("descricao")
            .HasMaxLength(500);

        builder.Property(e => e.Content)
            .HasColumnName("conteudo")
            .HasColumnType("jsonb");

        builder.Property(e => e.PlainText)
            .HasColumnName("textoplano");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasDefaultValue("automatica")
            .IsRequired();

        builder.Property(e => e.SourceHash)
            .HasColumnName("hashorigem")
            .HasMaxLength(64);

        // busca column — populated by trigger, not EF
        builder.Property<NpgsqlTypes.NpgsqlTsVector>("SearchVector")
            .HasColumnName("busca")
            .HasColumnType("tsvector");
        builder.HasIndex("SearchVector")
            .HasDatabaseName("idx_paginas_traducoes_busca")
            .HasMethod("gin");

        builder.Property(e => e.TranslatedByUserId)
            .HasColumnName("traduzidopor");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("criadoem")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("atualizadoem")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.DeletedAt)
            .HasColumnName("desativadoem")
            .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(e => e.Page)
            .WithMany()
            .HasForeignKey(e => e.PageId)
            .HasConstraintName("fk_paginas_traducoes_paginaid")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TranslatedByUser)
            .WithMany()
            .HasForeignKey(e => e.TranslatedByUserId)
            .HasConstraintName("fk_paginas_traducoes_traduzidopor")
            .OnDelete(DeleteBehavior.SetNull);

        // Unique: one active translation per page+language
        builder.HasIndex(e => new { e.PageId, e.Language })
            .HasDatabaseName("uq_paginas_traducoes_paginaid_idioma")
            .IsUnique()
            .HasFilter("desativadoem IS NULL");

        builder.HasIndex(e => e.PageId)
            .HasDatabaseName("idx_paginas_traducoes_paginaid");
    }
}
