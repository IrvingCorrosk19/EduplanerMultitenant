using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class RenameSubjectAssignmentSchoolIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_subject_assignments_SchoolId",
                table: "subject_assignments",
                newName: "IX_subject_assignments_school_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_subject_assignments_school_id",
                table: "subject_assignments",
                newName: "IX_subject_assignments_SchoolId");
        }
    }
}
