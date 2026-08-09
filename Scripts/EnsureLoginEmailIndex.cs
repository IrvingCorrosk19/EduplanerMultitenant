using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>Índice lower(email) para ResolveLoginSchools / GetByEmailForLoginAsync.</summary>
public static class EnsureLoginEmailIndex
{
    public static async Task EnsureAsync(SchoolDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(@"
CREATE INDEX IF NOT EXISTS ix_users_lower_email
    ON users (lower((email)::text));");
    }
}
