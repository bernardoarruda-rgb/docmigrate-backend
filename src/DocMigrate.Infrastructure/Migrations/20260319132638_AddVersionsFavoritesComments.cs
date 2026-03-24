using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionsFavoritesComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "paginas_comentarios",
                columns: table => new
                {
                    paginascomentariosid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paginaid = table.Column<int>(type: "integer", nullable: false),
                    conteudo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    autorid = table.Column<int>(type: "integer", nullable: true),
                    comentariopaiid = table.Column<int>(type: "integer", nullable: true),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    atualizadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    desativadoem = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paginas_comentarios", x => x.paginascomentariosid);
                    table.ForeignKey(
                        name: "fk_paginas_comentarios_autorid",
                        column: x => x.autorid,
                        principalTable: "usuarios",
                        principalColumn: "usuariosid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_paginas_comentarios_comentariopaiid",
                        column: x => x.comentariopaiid,
                        principalTable: "paginas_comentarios",
                        principalColumn: "paginascomentariosid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_paginas_comentarios_paginaid",
                        column: x => x.paginaid,
                        principalTable: "paginas",
                        principalColumn: "paginasid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paginas_favoritas",
                columns: table => new
                {
                    paginasfavoritasid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuarioid = table.Column<int>(type: "integer", nullable: false),
                    paginaid = table.Column<int>(type: "integer", nullable: false),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paginas_favoritas", x => x.paginasfavoritasid);
                    table.ForeignKey(
                        name: "fk_paginas_favoritas_paginaid",
                        column: x => x.paginaid,
                        principalTable: "paginas",
                        principalColumn: "paginasid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_paginas_favoritas_usuarioid",
                        column: x => x.usuarioid,
                        principalTable: "usuarios",
                        principalColumn: "usuariosid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paginas_versoes",
                columns: table => new
                {
                    paginasversoesid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paginaid = table.Column<int>(type: "integer", nullable: false),
                    versaonumero = table.Column<int>(type: "integer", nullable: false),
                    conteudo = table.Column<string>(type: "jsonb", nullable: false),
                    textoplano = table.Column<string>(type: "text", nullable: true),
                    criadoporusuarioid = table.Column<int>(type: "integer", nullable: true),
                    descricaomudanca = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    atualizadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    desativadoem = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paginas_versoes", x => x.paginasversoesid);
                    table.ForeignKey(
                        name: "fk_paginas_versoes_criadoporusuarioid",
                        column: x => x.criadoporusuarioid,
                        principalTable: "usuarios",
                        principalColumn: "usuariosid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_paginas_versoes_paginaid",
                        column: x => x.paginaid,
                        principalTable: "paginas",
                        principalColumn: "paginasid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paginas_visitas",
                columns: table => new
                {
                    paginasvisitasid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuarioid = table.Column<int>(type: "integer", nullable: false),
                    paginaid = table.Column<int>(type: "integer", nullable: false),
                    visitadoem = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_paginas_visitas", x => x.paginasvisitasid);
                    table.ForeignKey(
                        name: "fk_paginas_visitas_paginaid",
                        column: x => x.paginaid,
                        principalTable: "paginas",
                        principalColumn: "paginasid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_paginas_visitas_usuarioid",
                        column: x => x.usuarioid,
                        principalTable: "usuarios",
                        principalColumn: "usuariosid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_paginas_comentarios_autorid",
                table: "paginas_comentarios",
                column: "autorid");

            migrationBuilder.CreateIndex(
                name: "idx_paginas_comentarios_paginaid",
                table: "paginas_comentarios",
                column: "paginaid");

            migrationBuilder.CreateIndex(
                name: "IX_paginas_comentarios_comentariopaiid",
                table: "paginas_comentarios",
                column: "comentariopaiid");

            migrationBuilder.CreateIndex(
                name: "idx_paginas_favoritas_usuarioid",
                table: "paginas_favoritas",
                column: "usuarioid");

            migrationBuilder.CreateIndex(
                name: "IX_paginas_favoritas_paginaid",
                table: "paginas_favoritas",
                column: "paginaid");

            migrationBuilder.CreateIndex(
                name: "uq_paginas_favoritas_usuarioid_paginaid",
                table: "paginas_favoritas",
                columns: new[] { "usuarioid", "paginaid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_paginas_versoes_paginaid",
                table: "paginas_versoes",
                column: "paginaid");

            migrationBuilder.CreateIndex(
                name: "IX_paginas_versoes_criadoporusuarioid",
                table: "paginas_versoes",
                column: "criadoporusuarioid");

            migrationBuilder.CreateIndex(
                name: "uq_paginas_versoes_paginaid_versaonumero",
                table: "paginas_versoes",
                columns: new[] { "paginaid", "versaonumero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_paginas_visitas_usuarioid_visitadoem",
                table: "paginas_visitas",
                columns: new[] { "usuarioid", "visitadoem" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_paginas_visitas_paginaid",
                table: "paginas_visitas",
                column: "paginaid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paginas_comentarios");

            migrationBuilder.DropTable(
                name: "paginas_favoritas");

            migrationBuilder.DropTable(
                name: "paginas_versoes");

            migrationBuilder.DropTable(
                name: "paginas_visitas");
        }
    }
}
