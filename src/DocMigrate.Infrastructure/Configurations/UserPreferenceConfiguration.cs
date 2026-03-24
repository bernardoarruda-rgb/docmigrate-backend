using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("preferencias");

        builder.HasKey(e => e.Id).HasName("pk_preferencias");
        builder.Property(e => e.Id).HasColumnName("preferenciasid");

        builder.Property(e => e.UserId).HasColumnName("usuarioid").IsRequired();
        builder.HasIndex(e => e.UserId).IsUnique().HasFilter("desativadoem IS NULL").HasDatabaseName("uq_preferencias_usuarioid");

        builder.Property(e => e.Settings).HasColumnName("configuracoes").HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_preferencias_desativadoem");

        builder.HasOne(p => p.User)
            .WithOne(u => u.Preference)
            .HasForeignKey<UserPreference>(p => p.UserId)
            .HasConstraintName("fk_preferencias_usuarioid");
    }
}
