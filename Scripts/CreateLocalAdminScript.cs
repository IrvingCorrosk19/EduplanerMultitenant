using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Crea un usuario administrador en la BD local (admin@local.com / Admin123!).
/// Si no existe escuela, crea una "Escuela Local" y la asocia al admin.
/// Ejecutar: dotnet run -- --create-local-admin
/// </summary>
public static class CreateLocalAdminScript
{
    public const string AdminEmail = "admin@local.com";
    public const string AdminPassword = "Admin123!";

    public static async Task RunAsync(SchoolDbContext context)
    {
        var existingAdmin = await context.Users
            .FirstOrDefaultAsync(u => u.Email == AdminEmail && u.Role == "admin");
        if (existingAdmin != null)
        {
            existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Admin existente actualizado: {AdminEmail} / {AdminPassword}");
            return;
        }

        // Obtener o crear escuela
        var school = await context.Schools.FirstOrDefaultAsync(s => s.IsActive);
        if (school == null)
        {
            school = new School
            {
                Id = Guid.NewGuid(),
                Name = "Escuela Local",
                Address = "Dirección local",
                Phone = "+507 0000 0000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Schools.Add(school);
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Escuela creada: Escuela Local");
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            LastName = "Local",
            Email = AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
            Role = "admin",
            Status = "active",
            SchoolId = school.Id,
            DocumentId = "8-000-0001",
            DateOfBirth = new DateTime(1990, 1, 1),
            CellphonePrimary = "+507 0000 0000",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();

        school.AdminId = admin.Id;
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Admin creado: {AdminEmail} / {AdminPassword}");
        Console.WriteLine($"   Escuela: {school.Name} (Id: {school.Id})");
    }
}
