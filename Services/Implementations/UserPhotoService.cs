using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

/// <summary>
/// Servicio de aplicación: orquesta almacenamiento y entidad User (dominio).
/// </summary>
public sealed class UserPhotoService : IUserPhotoService
{
    private readonly SchoolDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<UserPhotoService> _logger;

    public UserPhotoService(
        SchoolDbContext context,
        IFileStorageService fileStorage,
        ILogger<UserPhotoService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task UpdatePhotoAsync(Guid userId, IFormFile file)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("[UserPhoto] Usuario no encontrado: {UserId}", userId);
            throw new InvalidOperationException("Usuario no encontrado.");
        }

        var previousPhotoUrl = user.PhotoUrl;

        var newPhotoUrl = await _fileStorage.SaveUserPhotoAsync(file, userId);
        user.UpdatePhoto(newPhotoUrl);
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(previousPhotoUrl) && previousPhotoUrl != newPhotoUrl)
        {
            await _fileStorage.DeleteUserPhotoAsync(previousPhotoUrl);
        }

        _logger.LogInformation("[UserPhoto] Foto actualizada UserId={UserId}", userId);
    }

    public async Task RemovePhotoAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("[UserPhoto] Usuario no encontrado: {UserId}", userId);
            throw new InvalidOperationException("Usuario no encontrado.");
        }

        var previousPhotoUrl = user.PhotoUrl;
        user.UpdatePhoto(null);
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(previousPhotoUrl))
        {
            await _fileStorage.DeleteUserPhotoAsync(previousPhotoUrl);
        }

        _logger.LogInformation("[UserPhoto] Foto eliminada UserId={UserId}", userId);
    }
}
