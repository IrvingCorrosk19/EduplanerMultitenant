using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleAndStudentIdCardStatusIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_student_id_cards_student_id_status",
                table: "student_id_cards",
                columns: new[] { "student_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_student_id_cards_student_id_status",
                table: "student_id_cards");
        }
    }
}
