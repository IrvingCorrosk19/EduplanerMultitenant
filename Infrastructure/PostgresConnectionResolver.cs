using System.Text;
using Microsoft.Extensions.Configuration;

namespace SchoolManager.Infrastructure;

/// <summary>
/// Resuelve la cadena Npgsql: appsettings, variable ConnectionStrings__DefaultConnection o DATABASE_URL (Render).
/// </summary>
public static class PostgresConnectionResolver
{
    public static string? Resolve(IConfiguration configuration)
    {
        var fromConfig = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return EnsurePooling(fromConfig);

        var fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return EnsurePooling(fromEnv);

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return EnsurePooling(ConvertDatabaseUrlToNpgsql(databaseUrl));

        return null;
    }

    public static string ConvertDatabaseUrlToNpgsql(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var path = uri.AbsolutePath.TrimStart('/');
        var db = path.Split('?')[0];
        if (string.IsNullOrEmpty(db))
            db = "postgres";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;

        var sb = new StringBuilder();
        sb.Append($"Host={host};Port={port};Username={user};Password={pass};Database={db};");

        var query = uri.Query ?? "";
        var needsSsl = query.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase)
                       || query.Contains("sslmode=verify-full", StringComparison.OrdinalIgnoreCase)
                       || query.Contains("sslmode=verify-ca", StringComparison.OrdinalIgnoreCase);

        if (!needsSsl && !IsLocalHost(host))
            needsSsl = true;

        if (needsSsl)
            sb.Append("SSL Mode=Require;Trust Server Certificate=true;");

        // Pool de conexiones enterprise (Npgsql defaults son bajos para SaaS multi-tenant)
        sb.Append("Pooling=true;Minimum Pool Size=2;Maximum Pool Size=100;Timeout=30;Connection Idle Lifetime=300;");

        return sb.ToString();
    }

    /// <summary>
    /// Enriquece una cadena Npgsql con parámetros de pool si aún no están definidos.
    /// No altera Host/Database/credenciales.
    /// </summary>
    public static string EnsurePooling(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (!connectionString.Contains("Maximum Pool Size", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("MaxPoolSize", StringComparison.OrdinalIgnoreCase))
        {
            csb.MaxPoolSize = Math.Max(csb.MaxPoolSize, 100);
        }
        if (!connectionString.Contains("Minimum Pool Size", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("MinPoolSize", StringComparison.OrdinalIgnoreCase))
        {
            csb.MinPoolSize = Math.Max(csb.MinPoolSize, 2);
        }
        csb.Pooling = true;
        if (csb.Timeout < 15)
            csb.Timeout = 30;
        return csb.ConnectionString;
    }

    private static bool IsLocalHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
}
