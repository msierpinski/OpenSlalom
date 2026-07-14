using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMeisterschaftLapTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fahrer_inder_meisterschaft",
                columns: table => new
                {
                    fk_id_meisterschaft = table.Column<int>(type: "int", nullable: false),
                    fk_id_fahrer = table.Column<int>(type: "int", nullable: false),
                    reihenfolge = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    updated_at_utc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fahrer_inder_meisterschaft", x => new { x.fk_id_meisterschaft, x.fk_id_fahrer });
                    table.ForeignKey(
                        name: "FK_fahrer_inder_meisterschaft_fahrer",
                        column: x => x.fk_id_fahrer,
                        principalTable: "fahrer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fahrer_inder_meisterschaft_meisterschaften",
                        column: x => x.fk_id_meisterschaft,
                        principalTable: "meisterschaften",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mstints",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    fk_id_meisterschaft = table.Column<int>(type: "int", nullable: false),
                    fk_id_fahrer = table.Column<int>(type: "int", nullable: false),
                    fk_id_kart = table.Column<int>(type: "int", nullable: true),
                    altersklasse_snapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    datum = table.Column<DateTime>(type: "datetime", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mstints", x => x.id);
                    table.ForeignKey(
                        name: "FK_mstints_fahrer",
                        column: x => x.fk_id_fahrer,
                        principalTable: "fahrer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mstints_karts",
                        column: x => x.fk_id_kart,
                        principalTable: "karts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_mstints_meisterschaften",
                        column: x => x.fk_id_meisterschaft,
                        principalTable: "meisterschaften",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mrunden",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    fk_id_mstint = table.Column<int>(type: "int", nullable: true),
                    runde = table.Column<int>(type: "int", nullable: true),
                    rundenzeit = table.Column<double>(type: "double", nullable: true),
                    pf = table.Column<int>(type: "int", nullable: true),
                    tf = table.Column<int>(type: "int", nullable: true),
                    ungueltig = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mrunden", x => x.id);
                    table.ForeignKey(
                        name: "FK_mrunden_mstints",
                        column: x => x.fk_id_mstint,
                        principalTable: "mstints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_fahrer_inder_meisterschaft_fk_id_fahrer",
                table: "fahrer_inder_meisterschaft",
                column: "fk_id_fahrer");

            migrationBuilder.CreateIndex(
                name: "IX_mrunden_fk_id_mstint",
                table: "mrunden",
                column: "fk_id_mstint");

            migrationBuilder.CreateIndex(
                name: "IX_mstints_fk_id_fahrer",
                table: "mstints",
                column: "fk_id_fahrer");

            migrationBuilder.CreateIndex(
                name: "IX_mstints_fk_id_kart",
                table: "mstints",
                column: "fk_id_kart");

            migrationBuilder.CreateIndex(
                name: "IX_mstints_fk_id_meisterschaft",
                table: "mstints",
                column: "fk_id_meisterschaft");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fahrer_inder_meisterschaft");

            migrationBuilder.DropTable(
                name: "mrunden");

            migrationBuilder.DropTable(
                name: "mstints");
        }
    }
}
