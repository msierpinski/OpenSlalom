using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddSyncTimestampIndexesSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_wetter_updated_at_utc",
                table: "wetter",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_vereine_updated_at_utc",
                table: "vereine",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_tstints_updated_at_utc",
                table: "tstints",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_trunden_updated_at_utc",
                table: "trunden",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_training_updated_at_utc",
                table: "training",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_mstints_updated_at_utc",
                table: "mstints",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_mrunden_updated_at_utc",
                table: "mrunden",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_meisterschaften_updated_at_utc",
                table: "meisterschaften",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_karts_updated_at_utc",
                table: "karts",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_fahrer_inder_meisterschaft_updated_at_utc",
                table: "fahrer_inder_meisterschaft",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_fahrer_im_training_updated_at_utc",
                table: "fahrer_im_training",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_fahrer_updated_at_utc",
                table: "fahrer",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_disziplin_altersklassen_updated_at_utc",
                table: "disziplin_altersklassen",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_disziplin_updated_at_utc",
                table: "disziplin",
                column: "updated_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_wetter_updated_at_utc",
                table: "wetter");

            migrationBuilder.DropIndex(
                name: "IX_vereine_updated_at_utc",
                table: "vereine");

            migrationBuilder.DropIndex(
                name: "IX_tstints_updated_at_utc",
                table: "tstints");

            migrationBuilder.DropIndex(
                name: "IX_trunden_updated_at_utc",
                table: "trunden");

            migrationBuilder.DropIndex(
                name: "IX_training_updated_at_utc",
                table: "training");

            migrationBuilder.DropIndex(
                name: "IX_mstints_updated_at_utc",
                table: "mstints");

            migrationBuilder.DropIndex(
                name: "IX_mrunden_updated_at_utc",
                table: "mrunden");

            migrationBuilder.DropIndex(
                name: "IX_meisterschaften_updated_at_utc",
                table: "meisterschaften");

            migrationBuilder.DropIndex(
                name: "IX_karts_updated_at_utc",
                table: "karts");

            migrationBuilder.DropIndex(
                name: "IX_fahrer_inder_meisterschaft_updated_at_utc",
                table: "fahrer_inder_meisterschaft");

            migrationBuilder.DropIndex(
                name: "IX_fahrer_im_training_updated_at_utc",
                table: "fahrer_im_training");

            migrationBuilder.DropIndex(
                name: "IX_fahrer_updated_at_utc",
                table: "fahrer");

            migrationBuilder.DropIndex(
                name: "IX_disziplin_altersklassen_updated_at_utc",
                table: "disziplin_altersklassen");

            migrationBuilder.DropIndex(
                name: "IX_disziplin_updated_at_utc",
                table: "disziplin");
        }
    }
}
