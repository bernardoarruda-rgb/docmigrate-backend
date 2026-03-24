using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPageLockingAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "bloqueadoem",
                table: "paginas",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bloqueadopor",
                table: "paginas",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "templates",
                columns: table => new
                {
                    templatesid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    titulo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    icone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    conteudo = table.Column<string>(type: "jsonb", nullable: true),
                    padrao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    atualizadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    desativadoem = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_templates", x => x.templatesid);
                });

            migrationBuilder.CreateIndex(
                name: "idx_templates_desativadoem",
                table: "templates",
                column: "desativadoem",
                filter: "desativadoem IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "templates");

            migrationBuilder.DropColumn(
                name: "bloqueadoem",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "bloqueadopor",
                table: "paginas");
        }
    }
}
