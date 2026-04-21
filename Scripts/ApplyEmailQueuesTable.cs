using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Crea <c>email_queues</c> y registra la migración en <c>__EFMigrationsHistory</c> (idempotente).
/// Uso local: <c>dotnet run -- --apply-email-queues-table</c> con <c>appsettings.Development.json</c>.
/// </summary>
public static class ApplyEmailQueuesTable
{
    public static async Task RunAsync(SchoolDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS email_queues (
                id uuid NOT NULL DEFAULT uuid_generate_v4(),
                user_id uuid NOT NULL,
                email character varying(255) NOT NULL,
                subject character varying(500),
                body text,
                status character varying(20) NOT NULL DEFAULT 'Pending',
                attempts integer NOT NULL DEFAULT 0,
                max_attempts integer NOT NULL DEFAULT 3,
                last_error character varying(2000),
                created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                processed_at timestamp with time zone,
                CONSTRAINT email_queues_pkey PRIMARY KEY (id),
                CONSTRAINT FK_email_queues_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
            );
            """);

        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_queues_created_at ON email_queues (created_at);");
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_queues_status ON email_queues (status);");
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_email_queues_user_id ON email_queues (user_id);");

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20260319112534_AddEmailQueueTable', '9.0.3')
            ON CONFLICT ("MigrationId") DO NOTHING;
            """);

        Console.WriteLine("✅ Tabla email_queues lista y migración registrada (si no existía).");
    }
}
