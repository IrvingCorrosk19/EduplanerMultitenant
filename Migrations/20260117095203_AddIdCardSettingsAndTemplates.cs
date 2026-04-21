using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddIdCardSettingsAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "id_card_template_fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    x_mm = table.Column<decimal>(type: "numeric(6,2)", nullable: false, defaultValue: 0m),
                    y_mm = table.Column<decimal>(type: "numeric(6,2)", nullable: false, defaultValue: 0m),
                    w_mm = table.Column<decimal>(type: "numeric(6,2)", nullable: false, defaultValue: 0m),
                    h_mm = table.Column<decimal>(type: "numeric(6,2)", nullable: false, defaultValue: 0m),
                    font_size = table.Column<decimal>(type: "numeric(4,2)", nullable: false, defaultValue: 10m),
                    font_weight = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Normal"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("id_card_template_fields_pkey", x => x.id);
                    table.ForeignKey(
                        name: "id_card_template_fields_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "school_id_card_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "default_v1"),
                    page_width_mm = table.Column<int>(type: "integer", nullable: false, defaultValue: 54),
                    page_height_mm = table.Column<int>(type: "integer", nullable: false, defaultValue: 86),
                    bleed_mm = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    background_color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#FFFFFF"),
                    primary_color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#0D6EFD"),
                    text_color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#111111"),
                    show_qr = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_photo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("school_id_card_settings_pkey", x => x.id);
                    table.ForeignKey(
                        name: "school_id_card_settings_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_id_card_template_fields_field",
                table: "id_card_template_fields",
                column: "field_key");

            migrationBuilder.CreateIndex(
                name: "ix_id_card_template_fields_school",
                table: "id_card_template_fields",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_school_id_card_settings_school_id",
                table: "school_id_card_settings",
                column: "school_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "id_card_template_fields");

            migrationBuilder.DropTable(
                name: "school_id_card_settings");
        }
    }
}
