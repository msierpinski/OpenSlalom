using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddTstintKartIdSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "fk_id_kart",
                table: "tstints",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tstints_fk_id_kart",
                table: "tstints",
                column: "fk_id_kart");

            migrationBuilder.AddForeignKey(
                name: "FK_tstints_karts",
                table: "tstints",
                column: "fk_id_kart",
                principalTable: "karts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tstints_karts",
                table: "tstints");

            migrationBuilder.DropIndex(
                name: "IX_tstints_fk_id_kart",
                table: "tstints");

            migrationBuilder.DropColumn(
                name: "fk_id_kart",
                table: "tstints");
        }
    }
}
