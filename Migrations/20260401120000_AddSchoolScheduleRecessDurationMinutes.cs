using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations;

/// <inheritdoc />
public partial class AddSchoolScheduleRecessDurationMinutes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "recess_duration_minutes",
            table: "school_schedule_configurations",
            type: "integer",
            nullable: false,
            defaultValue: 30);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "recess_duration_minutes",
            table: "school_schedule_configurations");
    }
}
