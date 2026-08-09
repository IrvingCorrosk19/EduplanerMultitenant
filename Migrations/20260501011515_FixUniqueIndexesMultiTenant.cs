using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueIndexesMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar índices únicos simples (si existen) — pueden tener diferentes nombres según el entorno
            migrationBuilder.Sql("DROP INDEX IF EXISTS specialties_name_key;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS grade_levels_name_key;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_grade_levels_school_id\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS area_name_key;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS activity_types_name_key;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_activity_types_school_id\";");

            // Crear índices únicos compuestos (school_id, name) para soporte multi-tenant correcto
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS specialties_school_name_key
                ON specialties (school_id, name);");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS grade_levels_school_name_key
                ON grade_levels (school_id, name);");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS area_school_name_key
                ON area (school_id, name);");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS activity_types_school_name_key
                ON activity_types (school_id, name);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "specialties_school_name_key",
                table: "specialties");

            migrationBuilder.DropIndex(
                name: "grade_levels_school_name_key",
                table: "grade_levels");

            migrationBuilder.DropIndex(
                name: "area_school_name_key",
                table: "area");

            migrationBuilder.DropIndex(
                name: "activity_types_school_name_key",
                table: "activity_types");

            migrationBuilder.CreateIndex(
                name: "specialties_name_key",
                table: "specialties",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "grade_levels_name_key",
                table: "grade_levels",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grade_levels_school_id",
                table: "grade_levels",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "area_name_key",
                table: "area",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "activity_types_name_key",
                table: "activity_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_activity_types_school_id",
                table: "activity_types",
                column: "school_id");
        }
    }
}
