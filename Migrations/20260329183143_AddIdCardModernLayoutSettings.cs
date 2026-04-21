using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddIdCardModernLayoutSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecondaryLogoUrl",
                table: "school_id_card_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowAcademicYear",
                table: "school_id_card_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowDocumentId",
                table: "school_id_card_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowPolicyNumber",
                table: "school_id_card_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowSecondaryLogo",
                table: "school_id_card_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseModernLayout",
                table: "school_id_card_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "email_queues",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            migrationBuilder.AddColumn<Guid>(
                name: "correlation_id",
                table: "email_queues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "email_queues",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "job_id",
                table: "email_queues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_at",
                table: "email_queues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locked_by",
                table: "email_queues",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until",
                table: "email_queues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at",
                table: "email_queues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_message_id",
                table: "email_queues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Accepted"),
                    total_items = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sent_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rejected_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    summary_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_jobs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_jobs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_queues_job_id",
                table: "email_queues",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_queues_locked_until",
                table: "email_queues",
                column: "locked_until");

            migrationBuilder.CreateIndex(
                name: "IX_email_queues_next_attempt_at",
                table: "email_queues",
                column: "next_attempt_at");

            migrationBuilder.CreateIndex(
                name: "IX_email_jobs_correlation_id",
                table: "email_jobs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_jobs_created_by_user_id",
                table: "email_jobs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_jobs_requested_at",
                table: "email_jobs",
                column: "requested_at");

            migrationBuilder.CreateIndex(
                name: "IX_email_jobs_status",
                table: "email_jobs",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_email_queues_email_jobs_job_id",
                table: "email_queues",
                column: "job_id",
                principalTable: "email_jobs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_queues_email_jobs_job_id",
                table: "email_queues");

            migrationBuilder.DropTable(
                name: "email_jobs");

            migrationBuilder.DropIndex(
                name: "IX_email_queues_job_id",
                table: "email_queues");

            migrationBuilder.DropIndex(
                name: "IX_email_queues_locked_until",
                table: "email_queues");

            migrationBuilder.DropIndex(
                name: "IX_email_queues_next_attempt_at",
                table: "email_queues");

            migrationBuilder.DropColumn(
                name: "SecondaryLogoUrl",
                table: "school_id_card_settings");

            migrationBuilder.DropColumn(
                name: "ShowAcademicYear",
                table: "school_id_card_settings");

            migrationBuilder.DropColumn(
                name: "ShowDocumentId",
                table: "school_id_card_settings");

            migrationBuilder.DropColumn(
                name: "ShowPolicyNumber",
                table: "school_id_card_settings");

            migrationBuilder.DropColumn(
                name: "ShowSecondaryLogo",
                table: "school_id_card_settings");

            migrationBuilder.DropColumn(
                name: "UseModernLayout",
                table: "school_id_card_settings");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "email_queues");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "email_queues");

            migrationBuilder.DropColumn(
                name: "job_id",
                table: "email_queues");

            migrationBuilder.DropColumn(
                name: "locked_at",
                table: "email_queues");

            migrationBuilder.DropColumn(
                name: "locked_by",
                table: "email_queues");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "email_queues");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "email_queues");

            migrationBuilder.DropColumn(
                name: "provider_message_id",
                table: "email_queues");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "email_queues",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Pending");
        }
    }
}
