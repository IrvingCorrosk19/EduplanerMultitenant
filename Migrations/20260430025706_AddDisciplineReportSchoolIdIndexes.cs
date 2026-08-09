using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddDisciplineReportSchoolIdIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_school_id",
                table: "discipline_reports",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_school_date",
                table: "discipline_reports",
                columns: new[] { "school_id", "date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_discipline_reports_school_date",
                table: "discipline_reports");

            migrationBuilder.DropIndex(
                name: "IX_discipline_reports_school_id",
                table: "discipline_reports");
        }
    }
}
