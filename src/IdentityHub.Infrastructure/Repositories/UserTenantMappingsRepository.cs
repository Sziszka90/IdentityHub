using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserTenantMappingsRepository"/>.
/// </summary>
public class UserTenantMappingsRepository : IUserTenantMappingsRepository
{
    private readonly IdentityHubDbContext _db;

    public UserTenantMappingsRepository(IdentityHubDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserTenantMapping>> GetAllUserTenantMappingsAsync(CancellationToken ct = default)
        => await _db.UserTenantMappings
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<UserTenantMapping?> GetUserTenantMappingByUserIdAsync(string userId, CancellationToken ct = default)
        => await _db.UserTenantMappings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == userId, ct);

    public UserTenantMapping? GetUserTenantMappingByUserId(string userId)
        => _db.UserTenantMappings
            .IgnoreQueryFilters()
            .FirstOrDefault(m => m.UserId == userId);

    public async Task<UserTenantMapping> UpsertUserTenantMappingAsync(string userId, CancellationToken ct = default)
    {
        var existing = await _db.UserTenantMappings
            .FirstOrDefaultAsync(m => m.UserId == userId, ct);

        if (existing is not null)
        {
            return existing;
        }

        var mapping = new UserTenantMapping { UserId = userId };
        _db.UserTenantMappings.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return mapping;
    }
}
