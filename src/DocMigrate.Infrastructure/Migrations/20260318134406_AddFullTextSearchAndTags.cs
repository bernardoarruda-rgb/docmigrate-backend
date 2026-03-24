using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchAndTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "textoplano",
                table: "paginas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "busca",
                table: "paginas",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('portuguese', coalesce(titulo, '') || ' ' || coalesce(descricao, '') || ' ' || coalesce(textoplano, ''))",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "busca",
                table: "espacos",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('portuguese', coalesce(titulo, '') || ' ' || coalesce(descricao, ''))",
                stored: true);

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    tagsid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    atualizadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    desativadoem = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.tagsid);
                });

            migrationBuilder.CreateTable(
                name: "espacos_tags",
                columns: table => new
                {
                    espacoid = table.Column<int>(type: "integer", nullable: false),
                    tagid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_espacos_tags", x => new { x.espacoid, x.tagid });
                    table.ForeignKey(
                        name: "fk_espacos_tags_espacoid",
                        column: x => x.espacoid,
                        principalTable: "espacos",
                        principalColumn: "espacosid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_espacos_tags_tagid",
                        column: x => x.tagid,
                        principalTable: "tags",
                        principalColumn: "tagsid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paginas_tags",
                columns: table => new
                {
                    paginaid = table.Column<int>(type: "integer", nullable: false),
                    tagid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paginas_tags", x => new { x.paginaid, x.tagid });
                    table.ForeignKey(
                        name: "fk_paginas_tags_paginaid",
                        column: x => x.paginaid,
                        principalTable: "paginas",
                        principalColumn: "paginasid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_paginas_tags_tagid",
                        column: x => x.tagid,
                        principalTable: "tags",
                        principalColumn: "tagsid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_paginas_busca",
                table: "paginas",
                column: "busca")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_espacos_busca",
                table: "espacos",
                column: "busca")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_espacos_tags_tagid",
                table: "espacos_tags",
                column: "tagid");

            migrationBuilder.CreateIndex(
                name: "IX_paginas_tags_tagid",
                table: "paginas_tags",
                column: "tagid");

            migrationBuilder.CreateIndex(
                name: "idx_tags_desativadoem",
                table: "tags",
                column: "desativadoem",
                filter: "desativadoem IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_tags_nome",
                table: "tags",
                column: "nome",
                unique: true,
                filter: "desativadoem IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "espacos_tags");

            migrationBuilder.DropTable(
                name: "paginas_tags");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropIndex(
                name: "idx_paginas_busca",
                table: "paginas");

            migrationBuilder.DropIndex(
                name: "idx_espacos_busca",
                table: "espacos");

            migrationBuilder.DropColumn(
                name: "busca",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "busca",
                table: "espacos");

            migrationBuilder.DropColumn(
                name: "textoplano",
                table: "paginas");
        }
    }
}
