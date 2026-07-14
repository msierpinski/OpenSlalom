using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddDisziplinAltersklassenSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disziplin_altersklassen",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_id_disziplin = table.Column<int>(type: "INTEGER", nullable: false),
                    bezeichnung = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    alter_von = table.Column<int>(type: "INTEGER", nullable: false),
                    alter_bis = table.Column<int>(type: "INTEGER", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disziplin_altersklassen", x => x.id);
                    table.ForeignKey(
                        name: "FK_disziplin_altersklassen_disziplin",
                        column: x => x.fk_id_disziplin,
                        principalTable: "disziplin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_disziplin_altersklassen_fk_id_disziplin",
                table: "disziplin_altersklassen",
                column: "fk_id_disziplin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disziplin_altersklassen");
        }
    }
}
