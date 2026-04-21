using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Crea <c>email_jobs</c> y agrega columnas operativas a <c>email_queues</c> (idempotente).
/// Se ejecuta automáticamente al iniciar la app (igual que ApplyEmailQueuesTable).
/// Uso manual: <c>dotnet run -- --apply-email-jobs</c>
/// </summary>
public static class ApplyEmailJobsAndQueueColumns
{
    public static async Task RunAsync(SchoolDbContext context)
    {
        // ── 1. Crear tabla email_jobs ─────────────────────────────────────────
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS email_jobs (
                id uuid NOT NULL DEFAULT uuid_generate_v4(),
                correlation_id uuid NOT NULL,
                created_by_user_id uuid NOT NULL,
                school_id uuid,
                requested_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                started_at timestamp with time zone,
                completed_at timestamp with time zone,
                status character varying(30) NOT NULL DEFAULT 'Accepted',
                total_items integer NOT NULL DEFAULT 0,
                sent_count integer NOT NULL DEFAULT 0,
                failed_count integer NOT NULL DEFAULT 0,
                rejected_count integer NOT NULL DEFAULT 0,
                summary_json text,
                CONSTRAINT email_jobs_pkey PRIMARY KEY (id),
                CONSTRAINT FK_email_jobs_users_created_by_user_id
                    FOREIGN KEY (created_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
            );
            """);

        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_jobs_correlation_id ON email_jobs (correlation_id);");
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_jobs_requested_at ON email_jobs (requested_at);");
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_jobs_status ON email_jobs (status);");

        // ── 2. Ampliar status en email_queues a varchar(30) ───────────────────
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE email_queues
                ALTER COLUMN status TYPE character varying(30);
            """);

        // ── 3. Agregar columnas nuevas a email_queues (idempotente) ───────────
        await AddColumnIfNotExists(context, "email_queues", "job_id",
            "uuid REFERENCES email_jobs(id) ON DELETE SET NULL");

        await AddColumnIfNotExists(context, "email_queues", "correlation_id", "uuid");
        await AddColumnIfNotExists(context, "email_queues", "locked_at",
            "timestamp with time zone");
        await AddColumnIfNotExists(context, "email_queues", "locked_until",
            "timestamp with time zone");
        await AddColumnIfNotExists(context, "email_queues", "locked_by",
            "character varying(100)");
        await AddColumnIfNotExists(context, "email_queues", "next_attempt_at",
            "timestamp with time zone");
        await AddColumnIfNotExists(context, "email_queues", "error_code",
            "character varying(50)");
        await AddColumnIfNotExists(context, "email_queues", "provider_message_id",
            "character varying(200)");

        // ── 4. Índices nuevos en email_queues ─────────────────────────────────
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_queues_job_id ON email_queues (job_id);");
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_queues_locked_until ON email_queues (locked_until);");
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_queues_next_attempt_at ON email_queues (next_attempt_at);");

        // ── 5. Registrar migración en __EFMigrationsHistory ───────────────────
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260319200000_AddEmailJobsAndQueueColumns', '9.0.3')
            ON CONFLICT ("MigrationId") DO NOTHING;
            """);

        Console.WriteLine("✅ email_jobs creada y columnas de email_queues actualizadas.");
    }

    private static async Task AddColumnIfNotExists(
        SchoolDbContext context,
        string table,
        string column,
        string definition)
    {
        // PostgreSQL: agrega columna solo si no existe (función disponible en PG 9.6+)
        await context.Database.ExecuteSqlRawAsync($"""
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name   = '{table}'
                      AND column_name  = '{column}'
                ) THEN
                    ALTER TABLE {table} ADD COLUMN {column} {definition};
                END IF;
            END
            $$;
            """);
    }
}
