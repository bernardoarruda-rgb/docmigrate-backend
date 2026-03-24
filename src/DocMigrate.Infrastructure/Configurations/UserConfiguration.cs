using DocMigrate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocMigrate.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(e => e.Id).HasName("pk_usuarios");
        builder.Property(e => e.Id).HasColumnName("usuariosid");

        builder.Property(e => e.KeycloakId).HasColumnName("keycloakid").HasMaxLength(255).IsRequired();
        builder.HasIndex(e => e.KeycloakId).IsUnique().HasDatabaseName("uq_usuarios_keycloakid");

        builder.Property(e => e.Name).HasColumnName("nome").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uq_usuarios_email");

        builder.Property(e => e.Role).HasColumnName("perfil").HasMaxLength(50).IsRequired().HasDefaultValue("admin");

        builder.Property(e => e.CreatedAt).HasColumnName("criadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("atualizadoem").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("desativadoem").HasColumnType("timestamptz");

        builder.HasIndex(e => e.DeletedAt).HasFilter("desativadoem IS NULL").HasDatabaseName("idx_usuarios_desativadoem");

        builder.HasOne(u => u.Preference)
            .WithOne(p => p.User)
            .HasForeignKey<UserPreference>(p => p.UserId);

        builder.HasData(new User
        {
            Id = 1,
            KeycloakId = "seed-admin",
            Name = "Administrador",
            Email = "admin@docmigrate.local",
            Role = "admin",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
