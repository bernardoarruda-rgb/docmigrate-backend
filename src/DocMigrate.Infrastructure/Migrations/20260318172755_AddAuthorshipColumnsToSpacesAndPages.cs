using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorshipColumnsToSpacesAndPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "atualizadoporusuarioid",
                table: "templates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "criadoporusuarioid",
                table: "templates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "atualizadoporusuarioid",
                table: "paginas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "criadoporusuarioid",
                table: "paginas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "atualizadoporusuarioid",
                table: "espacos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "criadoporusuarioid",
                table: "espacos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_templates_atualizadoporusuarioid",
                table: "templates",
                column: "atualizadoporusuarioid");

            migrationBuilder.CreateIndex(
                name: "IX_templates_criadoporusuarioid",
                table: "templates",
                column: "criadoporusuarioid");

            migrationBuilder.CreateIndex(
                name: "IX_paginas_atualizadoporusuarioid",
                table: "paginas",
                column: "atualizadoporusuarioid");

            migrationBuilder.CreateIndex(
                name: "IX_paginas_criadoporusuarioid",
                table: "paginas",
                column: "criadoporusuarioid");

            migrationBuilder.CreateIndex(
                name: "IX_espacos_atualizadoporusuarioid",
                table: "espacos",
                column: "atualizadoporusuarioid");

            migrationBuilder.CreateIndex(
                name: "IX_espacos_criadoporusuarioid",
                table: "espacos",
                column: "criadoporusuarioid");

            migrationBuilder.AddForeignKey(
                name: "fk_espacos_atualizadoporusuarioid",
                table: "espacos",
                column: "atualizadoporusuarioid",
                principalTable: "usuarios",
                principalColumn: "usuariosid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_espacos_criadoporusuarioid",
                table: "espacos",
                column: "criadoporusuarioid",
                principalTable: "usuarios",
                principalColumn: "usuariosid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_paginas_atualizadoporusuarioid",
                table: "paginas",
                column: "atualizadoporusuarioid",
                principalTable: "usuarios",
                principalColumn: "usuariosid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_paginas_criadoporusuarioid",
                table: "paginas",
                column: "criadoporusuarioid",
                principalTable: "usuarios",
                principalColumn: "usuariosid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_templates_atualizadoporusuarioid",
                table: "templates",
                column: "atualizadoporusuarioid",
                principalTable: "usuarios",
                principalColumn: "usuariosid",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_templates_criadoporusuarioid",
                table: "templates",
                column: "criadoporusuarioid",
                principalTable: "usuarios",
                principalColumn: "usuariosid",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_espacos_atualizadoporusuarioid",
                table: "espacos");

            migrationBuilder.DropForeignKey(
                name: "fk_espacos_criadoporusuarioid",
                table: "espacos");

            migrationBuilder.DropForeignKey(
                name: "fk_paginas_atualizadoporusuarioid",
                table: "paginas");

            migrationBuilder.DropForeignKey(
                name: "fk_paginas_criadoporusuarioid",
                table: "paginas");

            migrationBuilder.DropForeignKey(
                name: "fk_templates_atualizadoporusuarioid",
                table: "templates");

            migrationBuilder.DropForeignKey(
                name: "fk_templates_criadoporusuarioid",
                table: "templates");

            migrationBuilder.DropIndex(
                name: "IX_templates_atualizadoporusuarioid",
                table: "templates");

            migrationBuilder.DropIndex(
                name: "IX_templates_criadoporusuarioid",
                table: "templates");

            migrationBuilder.DropIndex(
                name: "IX_paginas_atualizadoporusuarioid",
                table: "paginas");

            migrationBuilder.DropIndex(
                name: "IX_paginas_criadoporusuarioid",
                table: "paginas");

            migrationBuilder.DropIndex(
                name: "IX_espacos_atualizadoporusuarioid",
                table: "espacos");

            migrationBuilder.DropIndex(
                name: "IX_espacos_criadoporusuarioid",
                table: "espacos");

            migrationBuilder.DropColumn(
                name: "atualizadoporusuarioid",
                table: "templates");

            migrationBuilder.DropColumn(
                name: "criadoporusuarioid",
                table: "templates");

            migrationBuilder.DropColumn(
                name: "atualizadoporusuarioid",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "criadoporusuarioid",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "atualizadoporusuarioid",
                table: "espacos");

            migrationBuilder.DropColumn(
                name: "criadoporusuarioid",
                table: "espacos");
        }
    }
}
