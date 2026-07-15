using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddVereinOrtSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ort",
                table: "vereine",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ort",
                table: "vereine");
        }
    }
}
