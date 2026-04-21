using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// Migración de sincronización de snapshot: las columnas ya fueron agregadas por migraciones previas.
        /// Esta migración solo actualiza el snapshot del modelo para que EF Core no reporte cambios pendientes.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: columnas ya creadas por AddIdCardOrientation y AddIdCardShowWatermark
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
        }
    }
}
