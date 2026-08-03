using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddMeisterschaftUuidSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "uuid",
                table: "meisterschaften",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE meisterschaften SET uuid = " +
                "lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-4' || " +
                "substr(lower(hex(randomblob(2))), 2) || '-' || " +
                "substr('89ab', (random() & 3) + 1, 1) || substr(lower(hex(randomblob(2))), 2) || '-' || " +
                "lower(hex(randomblob(6))) WHERE uuid IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "uuid",
                table: "meisterschaften",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

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
