using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPageLanguageAndTranslationHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hashorigem",
                table: "paginas_traducoes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idioma",
                table: "paginas",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "pt-BR");

            migrationBuilder.Sql("ALTER TABLE paginas ADD CONSTRAINT ck_paginas_idioma CHECK (idioma IN ('pt-BR', 'en', 'es'));");

            migrationBuilder.Sql(@"
                UPDATE paginas_traducoes pt
                SET hashorigem = encode(sha256(
                  (COALESCE(p.titulo, '') || '|' || COALESCE(p.descricao, '') || '|' || COALESCE(p.conteudo::text, ''))::bytea
                ), 'hex')
                FROM paginas p
                WHERE pt.paginaid = p.paginasid
                  AND pt.desativadoem IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hashorigem",
                table: "paginas_traducoes");

            migrationBuilder.DropColumn(
                name: "idioma",
                table: "paginas");
        }
    }
}
