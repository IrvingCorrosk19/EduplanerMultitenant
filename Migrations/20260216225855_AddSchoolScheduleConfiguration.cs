using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScheduleConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "school_schedule_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    morning_start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    morning_block_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    morning_block_count = table.Column<int>(type: "integer", nullable: false),
                    afternoon_start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    afternoon_block_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    afternoon_block_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("school_schedule_configurations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "school_schedule_configurations_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_school_schedule_configurations_school_id",
                table: "school_schedule_configurations",
                column: "school_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "school_schedule_configurations");
        }
    }
}
