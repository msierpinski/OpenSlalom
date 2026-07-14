using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeisterschaften : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meisterschaften",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    fk_id_gastgeber = table.Column<int>(type: "int", nullable: false),
                    fk_id_disziplin = table.Column<int>(type: "int", nullable: false),
                    fk_id_wetter = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    beschreibung = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    zeitpunkt = table.Column<DateOnly>(type: "date", nullable: false),
                    meisterschaft_abgeschlossen = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    aktiv_ausgerichtet = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
