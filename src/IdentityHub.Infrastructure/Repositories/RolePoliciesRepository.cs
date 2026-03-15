using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRolePoliciesRepository"/>.
/// </summary>
public class RolePoliciesRepository : IRolePoliciesRepository
{
    private readonly IdentityHubDbContext _db;

    public RolePoliciesRepository(IdentityHubDbContext db)
    {
        _db = db;
    }

    public async Task<List<RolePolicy>> GetAllRolePoliciesAsync(CancellationToken ct = default)
        => await _db.RolePolicies.AsNoTracking().OrderBy(p => p.PolicyName).ToListAsync(ct);

    public async Task<RolePolicy?> GetRolePolicyByNameAsync(string policyName, CancellationToken ct = default)
        => await _db.RolePolicies.FirstOrDefaultAsync(p => p.PolicyName == policyName, ct);

    public async Task<RolePolicy> CreateRolePolicyAsync(RolePolicy policy, CancellationToken ct = default)
    {
        _db.RolePolicies.Add(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<RolePolicy> UpdateRolePolicyAsync(RolePolicy policy, CancellationToken ct = default)
    {
        policy.UpdatedAt = DateTime.UtcNow;
        _db.RolePolicies.Update(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<bool> DeleteRolePolicyAsync(int id, CancellationToken ct = default)
    {
        var policy = await _db.RolePolicies.FindAsync([id], ct);
        if (policy is null)
        {
            return false;
        }

        _db.RolePolicies.Remove(policy);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
