using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecursivePageHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "nivel",
                table: "paginas",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "paginapaiid",
                table: "paginas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_paginas_paginapaiid",
                table: "paginas",
                column: "paginapaiid");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_paginas_paginapaiid",
                table: "paginas");

            migrationBuilder.DropIndex(
                name: "idx_paginas_paginapaiid",
                table: "paginas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_paginas_nivel",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "nivel",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "paginapaiid",
                table: "paginas");
        }
    }
}
