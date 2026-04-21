using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations;

/// <inheritdoc />
public partial class AddRecessAfterAfternoonBlockNumber : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "recess_after_afternoon_block_number",
            table: "school_schedule_configurations",
            type: "integer",
            nullable: false,
            defaultValue: 2);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "recess_after_afternoon_block_number",
            table: "school_schedule_configurations");
    }
}
