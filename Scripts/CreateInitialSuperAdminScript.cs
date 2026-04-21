using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Crea el superadmin inicial en la BD (superadmin@schoolmanager.com / Admin123!).
/// Ejecutar: dotnet run -- --create-initial-superadmin
/// </summary>
public static class CreateInitialSuperAdminScript
{
    public static async Task RunAsync(SchoolDbContext context)
    {
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Role == "superadmin");
        if (existing != null)
        {
            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            existing.Email = "superadmin@schoolmanager.com";
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Contraseña del superadmin actualizada: superadmin@schoolmanager.com / Admin123!");
            return;
        }

        var superAdmin = new User
        {
            Id = Guid.NewGuid(),
            Name = "Super",
            LastName = "Administrador",
            Email = "superadmin@schoolmanager.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "superadmin",
            Status = "active",
            SchoolId = null,
            DocumentId = "8-000-0000",
            DateOfBirth = new DateTime(1990, 1, 1),
            CellphonePrimary = "+507 0000 0000",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(superAdmin);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Superadmin creado: superadmin@schoolmanager.com / Admin123!");
    }
}
