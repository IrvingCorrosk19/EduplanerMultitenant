using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class EmailApiConfigurationReplaceResend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS resend_email_settings;");

            migrationBuilder.CreateTable(
                name: "email_api_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_api_configurations_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_api_configurations_is_active",
                table: "email_api_configurations",
                column: "is_active");

            migrationBuilder.Sql(
                """
                INSERT INTO email_api_configurations (id, provider, api_key, from_email, from_name, is_active, created_at)
                SELECT 'b2222222-2222-2222-2222-222222222222'::uuid, 'Resend', '', 'noreply@tusistema.com', 'SchoolManager', true, NOW()
                WHERE NOT EXISTS (SELECT 1 FROM email_api_configurations WHERE is_active = true);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_api_configurations");

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
        }
    }
}
