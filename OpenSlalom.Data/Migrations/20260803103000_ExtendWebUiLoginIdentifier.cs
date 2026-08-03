using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations;

[DbContext(typeof(OpenSlalomDbContext))]
[Migration("20260803103000_ExtendWebUiLoginIdentifier")]
public sealed class ExtendWebUiLoginIdentifier : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE web_login_attempts MODIFY COLUMN username VARCHAR(254) NOT NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE web_login_attempts MODIFY COLUMN username VARCHAR(100) NOT NULL;");
    }
}
