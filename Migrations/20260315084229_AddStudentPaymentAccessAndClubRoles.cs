using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPaymentAccessAndClubRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "teacher_work_plans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_user_id",
                table: "teacher_work_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rejected_at",
                table: "teacher_work_plans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rejected_by_user_id",
                table: "teacher_work_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review_comment",
                table: "teacher_work_plans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at",
                table: "teacher_work_plans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "student_payment_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carnet_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    platform_access_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    carnet_status_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    platform_status_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    carnet_updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    platform_updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("student_payment_access_pkey", x => x.id);
                    table.ForeignKey(
                        name: "student_payment_access_carnet_updated_by_fkey",
                        column: x => x.carnet_updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "student_payment_access_platform_updated_by_fkey",
                        column: x => x.platform_updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "student_payment_access_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "student_payment_access_student_id_fkey",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher_work_plan_review_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    teacher_work_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    performed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    comment = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("teacher_work_plan_review_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "teacher_work_plan_review_logs_plan_id_fkey",
                        column: x => x.teacher_work_plan_id,
                        principalTable: "teacher_work_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "teacher_work_plan_review_logs_user_fkey",
                        column: x => x.performed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plans_approved_by_user_id",
                table: "teacher_work_plans",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plans_rejected_by_user_id",
                table: "teacher_work_plans",
                column: "rejected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_payment_access_carnet_status_school_id",
                table: "student_payment_access",
                columns: new[] { "carnet_status", "school_id" });

            migrationBuilder.CreateIndex(
                name: "IX_student_payment_access_carnet_updated_by_user_id",
                table: "student_payment_access",
                column: "carnet_updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_payment_access_platform_updated_by_user_id",
                table: "student_payment_access",
                column: "platform_updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_payment_access_school_id",
                table: "student_payment_access",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_payment_access_student_id",
                table: "student_payment_access",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_payment_access_student_id_school_id",
                table: "student_payment_access",
                columns: new[] { "student_id", "school_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plan_review_logs_performed_by_user_id",
                table: "teacher_work_plan_review_logs",
                column: "performed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plan_review_logs_teacher_work_plan_id",
                table: "teacher_work_plan_review_logs",
                column: "teacher_work_plan_id");

            migrationBuilder.AddForeignKey(
                name: "teacher_work_plans_approved_by_fkey",
                table: "teacher_work_plans",
                column: "approved_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "teacher_work_plans_rejected_by_fkey",
                table: "teacher_work_plans",
                column: "rejected_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "teacher_work_plans_approved_by_fkey",
                table: "teacher_work_plans");

            migrationBuilder.DropForeignKey(
                name: "teacher_work_plans_rejected_by_fkey",
                table: "teacher_work_plans");

            migrationBuilder.DropTable(
                name: "student_payment_access");

            migrationBuilder.DropTable(
                name: "teacher_work_plan_review_logs");

            migrationBuilder.DropIndex(
                name: "IX_teacher_work_plans_approved_by_user_id",
                table: "teacher_work_plans");

            migrationBuilder.DropIndex(
                name: "IX_teacher_work_plans_rejected_by_user_id",
                table: "teacher_work_plans");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "teacher_work_plans");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "teacher_work_plans");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "teacher_work_plans");

            migrationBuilder.DropColumn(
                name: "rejected_by_user_id",
                table: "teacher_work_plans");

            migrationBuilder.DropColumn(
                name: "review_comment",
                table: "teacher_work_plans");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "teacher_work_plans");
        }
    }
}
