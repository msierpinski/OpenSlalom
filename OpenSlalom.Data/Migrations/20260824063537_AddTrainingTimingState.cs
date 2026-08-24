using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingTimingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "aktiver_fahrer_zeitnahme_1_id",
                table: "training",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "aktiver_fahrer_zeitnahme_2_id",
                table: "training",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "naechster_fahrer_zeitnahme_1_id",
                table: "training",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "naechster_fahrer_zeitnahme_2_id",
                table: "training",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aktiver_fahrer_zeitnahme_1_id",
                table: "training");

            migrationBuilder.DropColumn(
                name: "aktiver_fahrer_zeitnahme_2_id",
                table: "training");

            migrationBuilder.DropColumn(
                name: "naechster_fahrer_zeitnahme_1_id",
                table: "training");

            migrationBuilder.DropColumn(
                name: "naechster_fahrer_zeitnahme_2_id",
                table: "training");
        }
    }
}
