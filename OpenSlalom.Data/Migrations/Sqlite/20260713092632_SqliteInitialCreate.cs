using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class SqliteInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disziplin",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    disziplin = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disziplin", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vereine",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    vereinsname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    mitglieds_nummer = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vereine", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wetter",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    wetter = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wetter", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fahrer",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_id_verein = table.Column<int>(type: "INTEGER", nullable: false),
                    vorname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    nachname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fahrer", x => x.id);
                    table.ForeignKey(
                        name: "FK_fahrer_vereine",
                        column: x => x.fk_id_verein,
                        principalTable: "vereine",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "karts",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_id_verein = table.Column<int>(type: "INTEGER", nullable: false),
                    fk_id_disziplin = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Motor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Chassis = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_karts", x => x.id);
                    table.ForeignKey(
                        name: "FK_karts_disziplin",
                        column: x => x.fk_id_disziplin,
                        principalTable: "disziplin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_karts_vereine",
                        column: x => x.fk_id_verein,
                        principalTable: "vereine",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "training",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_id_verein = table.Column<int>(type: "INTEGER", nullable: false),
                    fk_id_disziplin = table.Column<int>(type: "INTEGER", nullable: false),
                    fk_id_wetter = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    beschreibung = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    zeitpunkt = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training", x => x.id);
                    table.ForeignKey(
                        name: "FK_training_disziplin",
                        column: x => x.fk_id_disziplin,
                        principalTable: "disziplin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_training_vereine",
                        column: x => x.fk_id_verein,
                        principalTable: "vereine",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_training_wetter",
                        column: x => x.fk_id_wetter,
                        principalTable: "wetter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fahrer_im_training",
                columns: table => new
                {
                    fk_id_training = table.Column<int>(type: "INTEGER", nullable: false),
                    fk_id_fahrer = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fahrer_im_training", x => new { x.fk_id_training, x.fk_id_fahrer });
                    table.ForeignKey(
                        name: "FK_fahrer_im_training_fahrer",
                        column: x => x.fk_id_fahrer,
                        principalTable: "fahrer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fahrer_im_training_training",
                        column: x => x.fk_id_training,
                        principalTable: "training",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tstints",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_id_training = table.Column<int>(type: "INTEGER", nullable: false),
                    fk_id_fahrer = table.Column<int>(type: "INTEGER", nullable: false),
                    datum = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tstints", x => x.id);
                    table.ForeignKey(
                        name: "FK_tstints_fahrer",
                        column: x => x.fk_id_fahrer,
                        principalTable: "fahrer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tstints_training",
                        column: x => x.fk_id_training,
                        principalTable: "training",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trunden",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fk_id_tstint = table.Column<int>(type: "INTEGER", nullable: true),
                    runde = table.Column<int>(type: "INTEGER", nullable: true),
                    rundenzeit = table.Column<double>(type: "REAL", nullable: true),
                    pf = table.Column<int>(type: "INTEGER", nullable: true),
                    tf = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trunden", x => x.id);
                    table.ForeignKey(
                        name: "FK_trunden_tstints",
                        column: x => x.fk_id_tstint,
                        principalTable: "tstints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fahrer_fk_id_verein",
                table: "fahrer",
                column: "fk_id_verein");

            migrationBuilder.CreateIndex(
                name: "IX_fahrer_im_training_fk_id_fahrer",
                table: "fahrer_im_training",
                column: "fk_id_fahrer");

            migrationBuilder.CreateIndex(
                name: "IX_karts_fk_id_disziplin",
                table: "karts",
                column: "fk_id_disziplin");

            migrationBuilder.CreateIndex(
                name: "IX_karts_fk_id_verein",
                table: "karts",
                column: "fk_id_verein");

            migrationBuilder.CreateIndex(
                name: "IX_training_fk_id_disziplin",
                table: "training",
                column: "fk_id_disziplin");

            migrationBuilder.CreateIndex(
                name: "IX_training_fk_id_verein",
                table: "training",
                column: "fk_id_verein");

            migrationBuilder.CreateIndex(
                name: "IX_training_fk_id_wetter",
                table: "training",
                column: "fk_id_wetter");

            migrationBuilder.CreateIndex(
                name: "IX_trunden_fk_id_tstint",
                table: "trunden",
                column: "fk_id_tstint");

            migrationBuilder.CreateIndex(
                name: "IX_tstints_fk_id_fahrer",
                table: "tstints",
                column: "fk_id_fahrer");

            migrationBuilder.CreateIndex(
                name: "IX_tstints_fk_id_training",
                table: "tstints",
                column: "fk_id_training");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fahrer_im_training");

            migrationBuilder.DropTable(
                name: "karts");

            migrationBuilder.DropTable(
                name: "trunden");

            migrationBuilder.DropTable(
                name: "tstints");

            migrationBuilder.DropTable(
                name: "fahrer");

            migrationBuilder.DropTable(
                name: "training");

            migrationBuilder.DropTable(
                name: "disziplin");

            migrationBuilder.DropTable(
                name: "vereine");

            migrationBuilder.DropTable(
                name: "wetter");
        }
    }
}
