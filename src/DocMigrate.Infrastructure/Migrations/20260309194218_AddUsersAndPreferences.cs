using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    usuariosid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    perfil = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "admin"),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    atualizadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    desativadoem = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.usuariosid);
                });

            migrationBuilder.CreateTable(
                name: "preferencias",
                columns: table => new
                {
                    preferenciasid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuarioid = table.Column<int>(type: "integer", nullable: false),
                    configuracoes = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    atualizadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    desativadoem = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preferencias", x => x.preferenciasid);
                    table.ForeignKey(
                        name: "fk_preferencias_usuarioid",
                        column: x => x.usuarioid,
                        principalTable: "usuarios",
                        principalColumn: "usuariosid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "usuariosid", "criadoem", "desativadoem", "email", "nome", "perfil", "atualizadoem" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@docmigrate.local", "Administrador", "admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "idx_preferencias_desativadoem",
                table: "preferencias",
                column: "desativadoem",
                filter: "desativadoem IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_preferencias_usuarioid",
                table: "preferencias",
                column: "usuarioid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_usuarios_desativadoem",
                table: "usuarios",
                column: "desativadoem",
                filter: "desativadoem IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "preferencias");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
