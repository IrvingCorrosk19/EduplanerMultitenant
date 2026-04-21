using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations;

/// <inheritdoc />
public partial class AddRecessAfterMorningBlockNumber : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "recess_after_morning_block_number",
            table: "school_schedule_configurations",
            type: "integer",
            nullable: false,
            defaultValue: 4);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "recess_after_morning_block_number",
            table: "school_schedule_configurations");
    }
}
