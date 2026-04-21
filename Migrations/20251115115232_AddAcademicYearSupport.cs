using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicYearSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "academic_year_id",
                table: "trimester",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "academic_year_id",
                table: "student_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "academic_year_id",
                table: "student_activity_scores",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "academic_years",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("academic_years_pkey", x => x.id);
                    table.ForeignKey(
                        name: "academic_years_created_by_fkey",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "academic_years_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "academic_years_updated_by_fkey",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trimester_academic_year_id",
                table: "trimester",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_assignments_academic_year_id",
                table: "student_assignments",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_assignments_student_academic_year",
                table: "student_assignments",
                columns: new[] { "student_id", "academic_year_id" });

            migrationBuilder.CreateIndex(
                name: "IX_student_assignments_student_active",
                table: "student_assignments",
                columns: new[] { "student_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_student_activity_scores_academic_year_id",
                table: "student_activity_scores",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_activity_scores_student_academic_year",
                table: "student_activity_scores",
                columns: new[] { "student_id", "academic_year_id" });

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_created_by",
                table: "academic_years",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_is_active",
                table: "academic_years",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_school_active",
                table: "academic_years",
                columns: new[] { "school_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_school_id",
                table: "academic_years",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_updated_by",
                table: "academic_years",
                column: "updated_by");

            migrationBuilder.AddForeignKey(
                name: "student_activity_scores_academic_year_id_fkey",
                table: "student_activity_scores",
                column: "academic_year_id",
                principalTable: "academic_years",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "student_assignments_academic_year_id_fkey",
                table: "student_assignments",
                column: "academic_year_id",
                principalTable: "academic_years",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "trimester_academic_year_id_fkey",
                table: "trimester",
                column: "academic_year_id",
                principalTable: "academic_years",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "student_activity_scores_academic_year_id_fkey",
                table: "student_activity_scores");

            migrationBuilder.DropForeignKey(
                name: "student_assignments_academic_year_id_fkey",
                table: "student_assignments");

            migrationBuilder.DropForeignKey(
                name: "trimester_academic_year_id_fkey",
                table: "trimester");

            migrationBuilder.DropTable(
                name: "academic_years");

            migrationBuilder.DropIndex(
                name: "IX_trimester_academic_year_id",
                table: "trimester");

            migrationBuilder.DropIndex(
                name: "IX_student_assignments_academic_year_id",
                table: "student_assignments");

            migrationBuilder.DropIndex(
                name: "IX_student_assignments_student_academic_year",
                table: "student_assignments");

            migrationBuilder.DropIndex(
                name: "IX_student_assignments_student_active",
                table: "student_assignments");

            migrationBuilder.DropIndex(
                name: "IX_student_activity_scores_academic_year_id",
                table: "student_activity_scores");

            migrationBuilder.DropIndex(
                name: "IX_student_activity_scores_student_academic_year",
                table: "student_activity_scores");

            migrationBuilder.DropColumn(
                name: "academic_year_id",
                table: "trimester");

            migrationBuilder.DropColumn(
                name: "academic_year_id",
                table: "student_assignments");

            migrationBuilder.DropColumn(
                name: "academic_year_id",
                table: "student_activity_scores");
        }
    }
}
