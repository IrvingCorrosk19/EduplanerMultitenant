using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterprisePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orientation_reports_school_id",
                table: "orientation_reports");

            migrationBuilder.CreateIndex(
                name: "IX_users_school_role",
                table: "users",
                columns: new[] { "school_id", "role" });

            migrationBuilder.CreateIndex(
                name: "IX_users_school_status",
                table: "users",
                columns: new[] { "school_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_student_assignments_school_group_grade_active",
                table: "student_assignments",
                columns: new[] { "school_id", "group_id", "grade_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_student_activity_scores_school_activity",
                table: "student_activity_scores",
                columns: new[] { "school_id", "activity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_school_period_status",
                table: "prematriculations",
                columns: new[] { "school_id", "prematriculation_period_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_school_status",
                table: "prematriculations",
                columns: new[] { "school_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_school_payment_date",
                table: "payments",
                columns: new[] { "school_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_school_payment_status",
                table: "payments",
                columns: new[] { "school_id", "payment_status" });

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_school_date",
                table: "orientation_reports",
                columns: new[] { "school_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_school_status",
                table: "orientation_reports",
                columns: new[] { "school_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_school_sent_at",
                table: "messages",
                columns: new[] { "school_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_school_group_date",
                table: "attendance",
                columns: new[] { "school_id", "group_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_school_student_date",
                table: "attendance",
                columns: new[] { "school_id", "student_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_school_group_grade",
                table: "activities",
                columns: new[] { "school_id", "group_id", "grade_level_id" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_school_subject_group_trimester",
                table: "activities",
                columns: new[] { "school_id", "subject_id", "group_id", "trimester" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_school_teacher_group",
                table: "activities",
                columns: new[] { "school_id", "teacher_id", "group_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_school_role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_school_status",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_student_assignments_school_group_grade_active",
                table: "student_assignments");

            migrationBuilder.DropIndex(
                name: "IX_student_activity_scores_school_activity",
                table: "student_activity_scores");

            migrationBuilder.DropIndex(
                name: "IX_prematriculations_school_period_status",
                table: "prematriculations");

            migrationBuilder.DropIndex(
                name: "IX_prematriculations_school_status",
                table: "prematriculations");

            migrationBuilder.DropIndex(
                name: "IX_payments_school_payment_date",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_school_payment_status",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_orientation_reports_school_date",
                table: "orientation_reports");

            migrationBuilder.DropIndex(
                name: "IX_orientation_reports_school_status",
                table: "orientation_reports");

            migrationBuilder.DropIndex(
                name: "IX_messages_school_sent_at",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_attendance_school_group_date",
                table: "attendance");

            migrationBuilder.DropIndex(
                name: "IX_attendance_school_student_date",
                table: "attendance");

            migrationBuilder.DropIndex(
                name: "IX_activities_school_group_grade",
                table: "activities");

            migrationBuilder.DropIndex(
                name: "IX_activities_school_subject_group_trimester",
                table: "activities");

            migrationBuilder.DropIndex(
                name: "IX_activities_school_teacher_group",
                table: "activities");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_school_id",
                table: "orientation_reports",
                column: "school_id");
        }
    }
}
