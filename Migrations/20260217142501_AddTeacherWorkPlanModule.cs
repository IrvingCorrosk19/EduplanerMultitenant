using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherWorkPlanModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teacher_work_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trimester = table.Column<int>(type: "integer", nullable: false),
                    objectives = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("teacher_work_plans_pkey", x => x.id);
                    table.ForeignKey(
                        name: "teacher_work_plans_academic_year_id_fkey",
                        column: x => x.academic_year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "teacher_work_plans_grade_level_id_fkey",
                        column: x => x.grade_level_id,
                        principalTable: "grade_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "teacher_work_plans_group_id_fkey",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "teacher_work_plans_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "teacher_work_plans_subject_id_fkey",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "teacher_work_plans_teacher_id_fkey",
                        column: x => x.teacher_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher_work_plan_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    teacher_work_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weeks_range = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    topic = table.Column<string>(type: "text", nullable: true),
                    conceptual_content = table.Column<string>(type: "text", nullable: true),
                    procedural_content = table.Column<string>(type: "text", nullable: true),
                    attitudinal_content = table.Column<string>(type: "text", nullable: true),
                    basic_competencies = table.Column<string>(type: "text", nullable: true),
                    achievement_indicators = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("teacher_work_plan_details_pkey", x => x.id);
                    table.ForeignKey(
                        name: "teacher_work_plan_details_plan_id_fkey",
                        column: x => x.teacher_work_plan_id,
                        principalTable: "teacher_work_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plan_details_teacher_work_plan_id",
                table: "teacher_work_plan_details",
                column: "teacher_work_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plans_academic_year_id",
                table: "teacher_work_plans",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plans_grade_level_id",
                table: "teacher_work_plans",
                column: "grade_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plans_group_id",
                table: "teacher_work_plans",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plans_school_id",
                table: "teacher_work_plans",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_work_plans_subject_id",
                table: "teacher_work_plans",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_work_plans_teacher_year_trim_subj_group",
                table: "teacher_work_plans",
                columns: new[] { "teacher_id", "academic_year_id", "trimester", "subject_id", "group_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teacher_work_plan_details");

            migrationBuilder.DropTable(
                name: "teacher_work_plans");
        }
    }
}
