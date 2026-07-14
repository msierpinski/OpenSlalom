using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddMeisterschaftenSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meisterschaften",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_id_gastgeber = table.Column<int>(type: "INTEGER", nullable: false),
                    fk_id_disziplin = table.Column<int>(type: "INTEGER", nullable: false),
                    fk_id_wetter = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    beschreibung = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    zeitpunkt = table.Column<DateOnly>(type: "date", nullable: false),
                    meisterschaft_abgeschlossen = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    aktiv_ausgerichtet = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meisterschaften", x => x.id);
                    table.ForeignKey(
                        name: "FK_meisterschaften_disziplin",
                        column: x => x.fk_id_disziplin,
                        principalTable: "disziplin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meisterschaften_vereine",
                        column: x => x.fk_id_gastgeber,
                        principalTable: "vereine",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meisterschaften_wetter",
                        column: x => x.fk_id_wetter,
                        principalTable: "wetter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_meisterschaften_fk_id_disziplin",
                table: "meisterschaften",
                column: "fk_id_disziplin");

            migrationBuilder.CreateIndex(
                name: "IX_meisterschaften_fk_id_gastgeber",
                table: "meisterschaften",
                column: "fk_id_gastgeber");

            migrationBuilder.CreateIndex(
                name: "IX_meisterschaften_fk_id_wetter",
                table: "meisterschaften",
                column: "fk_id_wetter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meisterschaften");
        }
    }
}
