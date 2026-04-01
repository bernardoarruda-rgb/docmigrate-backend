using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderEntityRemovePageHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_paginas_paginapaiid",
                table: "paginas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_paginas_nivel",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "nivel",
                table: "paginas");

            migrationBuilder.RenameColumn(
                name: "paginapaiid",
                table: "paginas",
                newName: "pastaid");

            migrationBuilder.RenameIndex(
                name: "idx_paginas_paginapaiid",
                table: "paginas",
                newName: "idx_paginas_pastaid");

            // Clear stale parent-page IDs — they pointed to paginas, not pastas
            migrationBuilder.Sql("UPDATE paginas SET pastaid = NULL WHERE pastaid IS NOT NULL;");

            migrationBuilder.CreateTable(
                name: "pastas",
                columns: table => new
                {
                    pastasid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    titulo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    icone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    coricone = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    ordem = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    nivel = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    espacoid = table.Column<int>(type: "integer", nullable: false),
                    pasta_paiid = table.Column<int>(type: "integer", nullable: true),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    atualizadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    desativadoem = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pastas", x => x.pastasid);
                    table.CheckConstraint("ck_pastas_nivel", "nivel BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_pastas_espacoid",
                        column: x => x.espacoid,
                        principalTable: "espacos",
                        principalColumn: "espacosid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pastas_pasta_paiid",
                        column: x => x.pasta_paiid,
                        principalTable: "pastas",
                        principalColumn: "pastasid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_pastas_desativadoem",
                table: "pastas",
                column: "desativadoem",
                filter: "desativadoem IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_pastas_espacoid",
                table: "pastas",
                column: "espacoid");

            migrationBuilder.CreateIndex(
                name: "idx_pastas_pasta_paiid",
                table: "pastas",
                column: "pasta_paiid");

            migrationBuilder.AddForeignKey(
                name: "fk_paginas_pastaid",
                table: "paginas",
                column: "pastaid",
                principalTable: "pastas",
                principalColumn: "pastasid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_paginas_pastaid",
                table: "paginas");

            migrationBuilder.DropTable(
                name: "pastas");

            migrationBuilder.RenameColumn(
                name: "pastaid",
                table: "paginas",
                newName: "paginapaiid");

            migrationBuilder.RenameIndex(
                name: "idx_paginas_pastaid",
                table: "paginas",
                newName: "idx_paginas_paginapaiid");

            migrationBuilder.AddColumn<int>(
                name: "nivel",
                table: "paginas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "ck_paginas_nivel",
                table: "paginas",
                sql: "nivel BETWEEN 1 AND 5");

            migrationBuilder.AddForeignKey(
                name: "fk_paginas_paginapaiid",
                table: "paginas",
                column: "paginapaiid",
                principalTable: "paginas",
                principalColumn: "paginasid",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
