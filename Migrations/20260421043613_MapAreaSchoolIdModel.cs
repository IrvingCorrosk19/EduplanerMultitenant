using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class MapAreaSchoolIdModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La columna `school_id` en `area` puede existir ya en bases creadas manualmente o por scripts.
            migrationBuilder.Sql("""
                ALTER TABLE area ADD COLUMN IF NOT EXISTS school_id uuid;
                CREATE INDEX IF NOT EXISTS "IX_area_school_id" ON area (school_id);
                DO $$
                BEGIN
                  IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'area_school_id_fkey'
                  ) THEN
                    ALTER TABLE area
                      ADD CONSTRAINT area_school_id_fkey
                      FOREIGN KEY (school_id) REFERENCES schools(id) ON DELETE SET NULL;
                  END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se elimina la columna: puede contener datos de tenant.
        }
    }
}
