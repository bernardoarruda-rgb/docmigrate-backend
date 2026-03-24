using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocMigrate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIconAndColorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "corfundo",
                table: "paginas",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "coricone",
                table: "paginas",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icone",
                table: "paginas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "corfundo",
                table: "espacos",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "coricone",
                table: "espacos",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icone",
                table: "espacos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "corfundo",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "coricone",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "icone",
                table: "paginas");

            migrationBuilder.DropColumn(
                name: "corfundo",
                table: "espacos");

            migrationBuilder.DropColumn(
                name: "coricone",
                table: "espacos");

            migrationBuilder.DropColumn(
                name: "icone",
                table: "espacos");
        }
    }
}
