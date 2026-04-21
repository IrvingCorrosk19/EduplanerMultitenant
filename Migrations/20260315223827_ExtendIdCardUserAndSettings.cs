using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class ExtendIdCardUserAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "allergies",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_name",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_phone",
                table: "users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_relationship",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_allergies",
                table: "school_id_card_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_emergency_contact",
                table: "school_id_card_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_school_phone",
                table: "school_id_card_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allergies",
                table: "users");

            migrationBuilder.DropColumn(
                name: "emergency_contact_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "emergency_contact_phone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "emergency_relationship",
                table: "users");

            migrationBuilder.DropColumn(
                name: "show_allergies",
                table: "school_id_card_settings");

            migrationBuilder.DropColumn(
                name: "show_emergency_contact",
                table: "school_id_card_settings");

            migrationBuilder.DropColumn(
                name: "show_school_phone",
                table: "school_id_card_settings");
        }
    }
}
