using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFahrerMitgliedsnummerUndVereinKontakt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "adresse",
                table: "vereine",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte[]>(
                name: "logo",
                table: "vereine",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postleitzahl",
                table: "vereine",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "mitglieds_nummer",
                table: "fahrer",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "adresse",
                table: "vereine");

            migrationBuilder.DropColumn(
                name: "logo",
                table: "vereine");

            migrationBuilder.DropColumn(
                name: "postleitzahl",
                table: "vereine");

            migrationBuilder.DropColumn(
                name: "mitglieds_nummer",
                table: "fahrer");
        }
    }
}
