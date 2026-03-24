using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class PageCommentConfiguration : IEntityTypeConfiguration<PageComment>
{
    public void Configure(EntityTypeBuilder<PageComment> builder)
    {
        builder.ToTable("paginas_comentarios");

        builder.HasKey(e => e.Id).HasName("pk_paginas_comentarios");
        builder.Property(e => e.Id).HasColumnName("paginascomentariosid");

        builder.Property(e => e.Content).HasColumnName("conteudo").HasMaxLength(2000).IsRequired();

        builder.Property(e => e.PageId).HasColumnName("paginaid");
        builder.HasOne(e => e.Page)
            .WithMany()
            .HasForeignKey(e => e.PageId)
            .HasConstraintName("fk_paginas_comentarios_paginaid")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.AuthorId).HasColumnName("autorid");
        builder.HasOne(e => e.Author)
            .WithMany()
            .HasForeignKey(e => e.AuthorId)
            .HasConstraintName("fk_paginas_comentarios_autorid")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.ParentCommentId).HasColumnName("comentariopaiid");
        builder.HasOne(e => e.ParentComment)
            .WithMany(e => e.Replies)
            .HasForeignKey(e => e.ParentCommentId)
            .HasConstraintName("fk_paginas_comentarios_comentariopaiid")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.PageId).HasDatabaseName("idx_paginas_comentarios_paginaid");
        builder.HasIndex(e => e.AuthorId).HasDatabaseName("idx_paginas_comentarios_autorid");

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
