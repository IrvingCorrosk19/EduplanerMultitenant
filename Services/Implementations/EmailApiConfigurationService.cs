using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class EmailApiConfigurationService : IEmailApiConfigurationService
{
    private readonly SchoolDbContext _db;

    public EmailApiConfigurationService(SchoolDbContext db)
    {
        _db = db;
    }

    public async Task<EmailApiConfiguration?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.EmailApiConfigurations.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
