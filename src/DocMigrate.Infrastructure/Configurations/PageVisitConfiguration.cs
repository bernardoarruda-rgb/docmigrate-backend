using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class PageVisitConfiguration : IEntityTypeConfiguration<PageVisit>
{
    public void Configure(EntityTypeBuilder<PageVisit> builder)
    {
        builder.ToTable("paginas_visitas");

        builder.HasKey(e => e.Id).HasName("pk_paginas_visitas");
        builder.Property(e => e.Id).HasColumnName("paginasvisitasid");

        builder.Property(e => e.UserId).HasColumnName("usuarioid");
        builder.Property(e => e.PageId).HasColumnName("paginaid");
        builder.Property(e => e.VisitedAt).HasColumnName("visitadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("fk_paginas_visitas_usuarioid")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Page)
            .WithMany()
            .HasForeignKey(e => e.PageId)
            .HasConstraintName("fk_paginas_visitas_paginaid")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.VisitedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_paginas_visitas_usuarioid_visitadoem");
    }
}
