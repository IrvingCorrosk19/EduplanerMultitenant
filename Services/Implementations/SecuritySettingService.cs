using SchoolManager.Models;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SchoolManager.Services.Implementations
{
public class SecuritySettingService : ISecuritySettingService
{
    private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SecuritySettingService(SchoolDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
            _currentUserService = currentUserService;
    }

    public async Task<List<SecuritySetting>> GetAllAsync() =>
        await _context.SecuritySettings.ToListAsync();

    public async Task<SecuritySetting?> GetBySchoolIdAsync(Guid schoolId)
    {
        return await _context.SecuritySettings
            .FirstOrDefaultAsync(s => s.SchoolId == schoolId);
    }

    public async Task CreateAsync(SecuritySetting setting)
    {
        _context.SecuritySettings.Add(setting);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SecuritySetting setting)
    {
        _context.SecuritySettings.Update(setting);
        await _context.SaveChangesAsync();
        }
    }
}
