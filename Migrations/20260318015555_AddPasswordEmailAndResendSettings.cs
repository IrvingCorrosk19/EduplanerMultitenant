using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordEmailAndResendSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "password_email_sent_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_email_status",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "resend_email_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    from_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("resend_email_settings_pkey", x => x.id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO resend_email_settings (id, api_key, from_address)
                VALUES ('a1111111-1111-1111-1111-111111111111', '', 'SchoolManager <noreply@tusistema.com>');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM resend_email_settings WHERE id = 'a1111111-1111-1111-1111-111111111111';");

            migrationBuilder.DropTable(
                name: "resend_email_settings");

            migrationBuilder.DropColumn(
                name: "password_email_sent_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_email_status",
                table: "users");
        }
    }
}
