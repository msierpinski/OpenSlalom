using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVereinOrt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ort",
                table: "vereine",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
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
