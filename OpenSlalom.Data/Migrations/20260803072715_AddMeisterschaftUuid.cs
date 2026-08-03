using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeisterschaftUuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "uuid",
                table: "meisterschaften",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.Sql("UPDATE meisterschaften SET uuid = UUID() WHERE uuid IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "uuid",
                table: "meisterschaften",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true,
                oldCollation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_meisterschaften_uuid",
                table: "meisterschaften",
                column: "uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_meisterschaften_uuid",
                table: "meisterschaften");

            migrationBuilder.DropColumn(
                name: "uuid",
                table: "meisterschaften");
        }
    }
}
