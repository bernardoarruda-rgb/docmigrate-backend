using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("pastas");
        builder.HasKey(e => e.Id).HasName("pk_pastas");
        builder.Property(e => e.Id).HasColumnName("pastasid");

        builder.Property(e => e.Title).HasColumnName("titulo").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Icon).HasColumnName("icone").HasMaxLength(500);
        builder.Property(e => e.IconColor).HasColumnName("coricone").HasMaxLength(7);
        builder.Property(e => e.SortOrder).HasColumnName("ordem").HasDefaultValue(0);
        builder.Property(e => e.Level).HasColumnName("nivel").HasDefaultValue(1);

        builder.HasCheckConstraint("ck_pastas_nivel", "nivel BETWEEN 1 AND 5");

        builder.Property(e => e.SpaceId).HasColumnName("espacoid");
        builder.HasOne(e => e.Space).WithMany(s => s.Folders).HasForeignKey(e => e.SpaceId).HasConstraintName("fk_pastas_espacoid").OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.ParentFolderId).HasColumnName("pasta_paiid");
        builder.HasOne(e => e.ParentFolder).WithMany(e => e.ChildFolders).HasForeignKey(e => e.ParentFolderId).HasConstraintName("fk_pastas_pasta_paiid").OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.SpaceId).HasDatabaseName("idx_pastas_espacoid");
        builder.HasIndex(e => e.ParentFolderId).HasDatabaseName("idx_pastas_pasta_paiid");
        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_pastas_desativadoem");
    }
}
