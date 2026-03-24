using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("templates");

        builder.HasKey(e => e.Id).HasName("pk_templates");
        builder.Property(e => e.Id).HasColumnName("templatesid");

        builder.Property(e => e.Title).HasColumnName("titulo").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Description).HasColumnName("descricao").HasMaxLength(500);
        builder.Property(e => e.Icon).HasColumnName("icone").HasMaxLength(500);
        builder.Property(e => e.Content).HasColumnName("conteudo").HasColumnType("jsonb");
        builder.Property(e => e.IsDefault).HasColumnName("padrao").HasDefaultValue(false);
        builder.Property(e => e.SortOrder).HasColumnName("ordem").HasDefaultValue(0);

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.Property(e => e.CreatedByUserId).HasColumnName("criadoporusuarioid");
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .HasConstraintName("fk_templates_criadoporusuarioid")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.UpdatedByUserId).HasColumnName("atualizadoporusuarioid");
        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .HasConstraintName("fk_templates_atualizadoporusuarioid")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_templates_desativadoem");
    }
}
