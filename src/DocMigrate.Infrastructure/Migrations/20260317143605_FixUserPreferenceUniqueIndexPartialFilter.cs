using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUserPreferenceUniqueIndexPartialFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_preferencias_usuarioid",
                table: "preferencias");

            migrationBuilder.CreateIndex(
                name: "uq_preferencias_usuarioid",
                table: "preferencias",
                column: "usuarioid",
                unique: true,
                filter: "desativadoem IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_preferencias_usuarioid",
                table: "preferencias");

            migrationBuilder.CreateIndex(
                name: "uq_preferencias_usuarioid",
                table: "preferencias",
                column: "usuarioid",
                unique: true);
        }
    }
}
