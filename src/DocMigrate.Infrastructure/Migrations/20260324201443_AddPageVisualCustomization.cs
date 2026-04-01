using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPageVisualCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "atribuicaocapa",
                table: "paginas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "larguraconteudo",
                table: "paginas",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "normal");

            migrationBuilder.AddColumn<int>(
                name: "posicaocapa",
                table: "paginas",
                type: "integer",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<string>(
                name: "tipocapa",
                table: "paginas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "valorcapa",
                table: "paginas",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_paginas_larguraconteudo",
                table: "paginas",
                sql: "larguraconteudo IN ('normal', 'wide', 'full')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_paginas_tipocapa",
                table: "paginas",
                sql: "tipocapa IN ('gradient', 'solid', 'image', 'unsplash') OR tipocapa IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_paginas_larguraconteudo",
                table: "paginas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_paginas_tipocapa",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "atribuicaocapa",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "larguraconteudo",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "posicaocapa",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "tipocapa",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "valorcapa",
                table: "paginas");
        }
    }
}
