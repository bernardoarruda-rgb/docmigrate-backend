using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKeycloakIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "keycloakid",
                table: "usuarios",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "paginas_traducoes",
                columns: table => new
                {
                    paginastraducoesid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paginaid = table.Column<int>(type: "integer", nullable: false),
                    idioma = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    titulo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    conteudo = table.Column<string>(type: "jsonb", nullable: true),
                    textoplano = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "automatica"),
                    traduzidopor = table.Column<int>(type: "integer", nullable: true),
                    busca = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    atualizadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    desativadoem = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paginas_traducoes", x => x.paginastraducoesid);
                    table.ForeignKey(
                        name: "fk_paginas_traducoes_paginaid",
                        column: x => x.paginaid,
                        principalTable: "paginas",
                        principalColumn: "paginasid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paginas_traducoes_traduzidopor",
                        column: x => x.traduzidopor,
                        principalTable: "usuarios",
                        principalColumn: "usuariosid",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "usuariosid",
                keyValue: 1,
                column: "keycloakid",
                value: "seed-admin");

            migrationBuilder.CreateIndex(
                name: "uq_usuarios_keycloakid",
                table: "usuarios",
                column: "keycloakid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_paginas_traducoes_busca",
                table: "paginas_traducoes",
                column: "busca")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_paginas_traducoes_paginaid",
                table: "paginas_traducoes",
                column: "paginaid");

            migrationBuilder.CreateIndex(
                name: "IX_paginas_traducoes_traduzidopor",
                table: "paginas_traducoes",
                column: "traduzidopor");

            migrationBuilder.CreateIndex(
                name: "uq_paginas_traducoes_paginaid_idioma",
                table: "paginas_traducoes",
                columns: new[] { "paginaid", "idioma" },
                unique: true,
                filter: "desativadoem IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paginas_traducoes");

            migrationBuilder.DropIndex(
                name: "uq_usuarios_keycloakid",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "keycloakid",
                table: "usuarios");
        }
    }
}
