using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations;

/// <inheritdoc />
public partial class IdCardPageDefaults55x85Mm : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Sincronizar columnas de carnet si faltaban en BD (idempotente).
        migrationBuilder.Sql(
            "ALTER TABLE student_id_cards ADD COLUMN IF NOT EXISTS is_printed boolean NOT NULL DEFAULT false;");
        migrationBuilder.Sql(
            "ALTER TABLE student_id_cards ADD COLUMN IF NOT EXISTS printed_at timestamp with time zone NULL;");

        migrationBuilder.AlterColumn<int>(
            name: "page_width_mm",
            table: "school_id_card_settings",
            type: "integer",
            nullable: false,
            defaultValue: 55,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 54);

        migrationBuilder.AlterColumn<int>(
            name: "page_height_mm",
            table: "school_id_card_settings",
            type: "integer",
            nullable: false,
            defaultValue: 85,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 86);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "page_width_mm",
            table: "school_id_card_settings",
            type: "integer",
            nullable: false,
            defaultValue: 54,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 55);

        migrationBuilder.AlterColumn<int>(
            name: "page_height_mm",
            table: "school_id_card_settings",
            type: "integer",
            nullable: false,
            defaultValue: 86,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 85);

        // No se eliminan is_printed / printed_at: podrían existir antes de esta migración y borrarías datos.
    }
}
