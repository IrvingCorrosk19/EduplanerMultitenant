using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyEmailConfigurationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tabla legacy no mapeada por EF; la app usa `email_configurations`. Evita duplicidad y columna "SchoolId".
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""EmailConfigurations"" CASCADE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se recrea la tabla legacy: no formaba parte del modelo actual.
        }
    }
}
