using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddFahrerMitgliedsnummerUndVereinKontaktSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "adresse",
                table: "vereine",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "logo",
                table: "vereine",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postleitzahl",
                table: "vereine",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "mitglieds_nummer",
                table: "fahrer",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
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
