using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations;

[DbContext(typeof(OpenSlalomDbContext))]
[Migration("20260803101500_AddWebUiEmailAndPasswordReset")]
public sealed class AddWebUiEmailAndPasswordReset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE web_users
                ADD COLUMN email VARCHAR(254) NULL AFTER username,
                ADD CONSTRAINT UX_web_users_email UNIQUE (email);
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE web_password_reset_tokens (
                id BIGINT NOT NULL AUTO_INCREMENT,
                user_id INT NOT NULL,
                token_hash CHAR(64) NOT NULL,
                expires_at_utc DATETIME NOT NULL,
                used_at_utc DATETIME NULL,
                created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                CONSTRAINT UX_web_password_reset_tokens_hash UNIQUE (token_hash),
                CONSTRAINT FK_web_password_reset_tokens_user FOREIGN KEY (user_id) REFERENCES web_users(id) ON DELETE CASCADE,
                INDEX IX_web_password_reset_tokens_user_expiry (user_id, expires_at_utc)
            ) CHARACTER SET utf8mb4;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE web_password_reset_tokens;");
        migrationBuilder.Sql("ALTER TABLE web_users DROP INDEX UX_web_users_email, DROP COLUMN email;");
    }
}
