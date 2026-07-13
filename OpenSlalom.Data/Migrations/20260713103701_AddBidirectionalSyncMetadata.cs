using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBidirectionalSyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "wetter",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "wetter",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "wetter",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "vereine",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "vereine",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "vereine",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "tstints",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tstints",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "tstints",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "trunden",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "trunden",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "trunden",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "training",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "training",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "training",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "karts",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "karts",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "karts",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "fahrer_im_training",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "fahrer_im_training",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "fahrer_im_training",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "fahrer",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "fahrer",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "fahrer",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "disziplin",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "disziplin",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "disziplin",
                type: "datetime",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateTable(
                name: "sync_state",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_sync_utc = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_state", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_state");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "wetter");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "wetter");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "wetter");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "vereine");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "vereine");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "vereine");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "tstints");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tstints");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "tstints");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "trunden");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "trunden");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "trunden");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "training");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "training");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "training");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "karts");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "karts");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "karts");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "fahrer_im_training");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "fahrer_im_training");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "fahrer_im_training");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "fahrer");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "fahrer");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "fahrer");

            migrationBuilder.DropColumn(
                name: "deleted_at_utc",
                table: "disziplin");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "disziplin");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "disziplin");
        }
    }
}
