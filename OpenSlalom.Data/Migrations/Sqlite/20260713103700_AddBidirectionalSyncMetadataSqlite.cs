using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddBidirectionalSyncMetadataSqlite : Migration
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
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "wetter",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "vereine",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "vereine",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "vereine",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "tstints",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tstints",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "tstints",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "trunden",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "trunden",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "trunden",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "training",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "training",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "training",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "karts",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "karts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "karts",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "fahrer_im_training",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "fahrer_im_training",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "fahrer_im_training",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "fahrer",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "fahrer",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "fahrer",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at_utc",
                table: "disziplin",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "disziplin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "disziplin",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.Sql("UPDATE wetter SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");
            migrationBuilder.Sql("UPDATE vereine SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");
            migrationBuilder.Sql("UPDATE tstints SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");
            migrationBuilder.Sql("UPDATE trunden SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");
            migrationBuilder.Sql("UPDATE training SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");
            migrationBuilder.Sql("UPDATE karts SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");
            migrationBuilder.Sql("UPDATE fahrer_im_training SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");
            migrationBuilder.Sql("UPDATE fahrer SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");
            migrationBuilder.Sql("UPDATE disziplin SET updated_at_utc = CURRENT_TIMESTAMP WHERE updated_at_utc = '1970-01-01 00:00:00';");

            migrationBuilder.CreateTable(
                name: "sync_state",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    last_sync_utc = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_state", x => x.id);
                });
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
