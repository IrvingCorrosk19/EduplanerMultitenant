using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class FixSubjectAssignmentSchoolIdColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subject_assignments_schools_SchoolId",
                table: "subject_assignments");

            migrationBuilder.RenameColumn(
                name: "SchoolId",
                table: "subject_assignments",
                newName: "school_id");

            migrationBuilder.AddForeignKey(
                name: "FK_subject_assignments_schools_school_id",
                table: "subject_assignments",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subject_assignments_schools_school_id",
                table: "subject_assignments");

            migrationBuilder.RenameColumn(
                name: "school_id",
                table: "subject_assignments",
                newName: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_subject_assignments_schools_SchoolId",
                table: "subject_assignments",
                column: "SchoolId",
                principalTable: "schools",
                principalColumn: "id");
        }
    }
}
