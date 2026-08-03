using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations;

[DbContext(typeof(OpenSlalomDbContext))]
[Migration("20260803104500_AddWebUiRegistration")]
public sealed class AddWebUiRegistration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "INSERT INTO web_roles (name, display_name) VALUES ('Registriert', 'Registriert');");

        migrationBuilder.Sql(
            """
            CREATE TABLE web_registration_attempts (
                id BIGINT NOT NULL AUTO_INCREMENT,
                ip_address VARCHAR(45) NOT NULL,
                attempted_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                INDEX IX_web_registration_attempts_ip_time (ip_address, attempted_at_utc)
            ) CHARACTER SET utf8mb4;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE web_registration_attempts;");
        migrationBuilder.Sql("DELETE ur FROM web_user_roles ur INNER JOIN web_roles r ON r.id = ur.role_id WHERE r.name = 'Registriert';");
        migrationBuilder.Sql("DELETE FROM web_roles WHERE name = 'Registriert';");
    }
}
