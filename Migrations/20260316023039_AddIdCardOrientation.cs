using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddIdCardOrientation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "orientation",
                table: "school_id_card_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Vertical");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "orientation",
                table: "school_id_card_settings");
        }
    }
}
