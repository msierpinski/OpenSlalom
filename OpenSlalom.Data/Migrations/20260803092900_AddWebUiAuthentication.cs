using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSlalom.Data.Migrations;

[DbContext(typeof(OpenSlalomDbContext))]
[Migration("20260803092900_AddWebUiAuthentication")]
public sealed class AddWebUiAuthentication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE web_roles (
                id INT NOT NULL AUTO_INCREMENT,
                name VARCHAR(32) NOT NULL,
                display_name VARCHAR(100) NOT NULL,
                PRIMARY KEY (id),
                CONSTRAINT UX_web_roles_name UNIQUE (name)
            ) CHARACTER SET utf8mb4;
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE web_users (
                id INT NOT NULL AUTO_INCREMENT,
                username VARCHAR(100) NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                fahrer_id INT NULL,
                is_active TINYINT(1) NOT NULL DEFAULT 1,
                created_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_login_at_utc DATETIME NULL,
                session_version INT NOT NULL DEFAULT 1,
                PRIMARY KEY (id),
                CONSTRAINT UX_web_users_username UNIQUE (username),
                CONSTRAINT UX_web_users_fahrer_id UNIQUE (fahrer_id),
                CONSTRAINT FK_web_users_fahrer FOREIGN KEY (fahrer_id) REFERENCES fahrer(id) ON DELETE RESTRICT
            ) CHARACTER SET utf8mb4;
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE web_user_roles (
                user_id INT NOT NULL,
                role_id INT NOT NULL,
                PRIMARY KEY (user_id, role_id),
                CONSTRAINT FK_web_user_roles_user FOREIGN KEY (user_id) REFERENCES web_users(id) ON DELETE CASCADE,
                CONSTRAINT FK_web_user_roles_role FOREIGN KEY (role_id) REFERENCES web_roles(id) ON DELETE RESTRICT
            ) CHARACTER SET utf8mb4;
            """);

        migrationBuilder.Sql(
            """
            CREATE TABLE web_login_attempts (
                id BIGINT NOT NULL AUTO_INCREMENT,
                username VARCHAR(100) NOT NULL,
                ip_address VARCHAR(45) NOT NULL,
                attempted_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                INDEX IX_web_login_attempts_username_time (username, attempted_at_utc),
                INDEX IX_web_login_attempts_ip_time (ip_address, attempted_at_utc)
            ) CHARACTER SET utf8mb4;
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO web_roles (name, display_name) VALUES
                ('Administrator', 'Administrator'),
                ('Trainingsleiter', 'Trainingsleiter'),
                ('Fahrer', 'Fahrer');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE web_login_attempts;");
        migrationBuilder.Sql("DROP TABLE web_user_roles;");
        migrationBuilder.Sql("DROP TABLE web_users;");
        migrationBuilder.Sql("DROP TABLE web_roles;");
    }
}
