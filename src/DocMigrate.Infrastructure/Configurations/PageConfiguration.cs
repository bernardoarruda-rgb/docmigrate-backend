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

        builder.Property(e => e.Icon).HasColumnName("icone").HasMaxLength(500);
        builder.Property(e => e.IconColor).HasColumnName("coricone").HasMaxLength(7);
        builder.Property(e => e.BackgroundColor).HasColumnName("corfundo").HasMaxLength(7);

        builder.Property(e => e.Language).HasColumnName("idioma").HasMaxLength(5).HasDefaultValue("pt-BR");

        builder.Property(e => e.LockedBy).HasColumnName("bloqueadopor").HasMaxLength(255);
        builder.Property(e => e.LockedAt).HasColumnName("bloqueadoem").HasColumnType("timestamptz");

        builder.Property(e => e.SpaceId).HasColumnName("espacoid");

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.Property(e => e.PlainText).HasColumnName("textoplano");

        builder.Property(e => e.SearchVector)
            .HasColumnName("busca")
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "to_tsvector('portuguese', coalesce(titulo, '') || ' ' || coalesce(descricao, '') || ' ' || coalesce(textoplano, ''))",
                stored: true);

        builder.HasIndex(e => e.SearchVector)
            .HasMethod("gin")
            .HasDatabaseName("idx_paginas_busca");

        builder.Property(e => e.CreatedByUserId).HasColumnName("criadoporusuarioid");
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .HasConstraintName("fk_paginas_criadoporusuarioid")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.UpdatedByUserId).HasColumnName("atualizadoporusuarioid");
        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .HasConstraintName("fk_paginas_atualizadoporusuarioid")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_paginas_desativadoem");
    }
}
