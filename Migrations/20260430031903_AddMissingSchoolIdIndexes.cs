using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingSchoolIdIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_attendance_school_id",
                table: "attendance",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_specialties_school_id",
                table: "specialties",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_activity_scores_school_id",
                table: "student_activity_scores",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_jobs_school_id",
                table: "email_jobs",
                column: "school_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendance_school_id",
                table: "attendance");

            migrationBuilder.DropIndex(
                name: "IX_specialties_school_id",
                table: "specialties");

            migrationBuilder.DropIndex(
                name: "IX_student_activity_scores_school_id",
                table: "student_activity_scores");

            migrationBuilder.DropIndex(
                name: "IX_email_jobs_school_id",
                table: "email_jobs");
        }
    }
}
