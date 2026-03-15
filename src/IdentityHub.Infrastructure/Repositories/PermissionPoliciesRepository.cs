using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPermissionPoliciesRepository"/>.
/// </summary>
public class PermissionPoliciesRepository : IPermissionPoliciesRepository
{
    private readonly IdentityHubDbContext _db;

    public PermissionPoliciesRepository(IdentityHubDbContext db)
    {
        _db = db;
    }

    public async Task<List<PermissionPolicy>> GetAllPermissionPoliciesAsync(CancellationToken ct = default)
        => await _db.PermissionPolicies.AsNoTracking().OrderBy(p => p.PolicyName).ToListAsync(ct);

    public async Task<PermissionPolicy?> GetPermissionPolicyByNameAsync(string policyName, CancellationToken ct = default)
        => await _db.PermissionPolicies.FirstOrDefaultAsync(p => p.PolicyName == policyName, ct);

    public async Task<PermissionPolicy> CreatePermissionPolicyAsync(PermissionPolicy policy, CancellationToken ct = default)
    {
        _db.PermissionPolicies.Add(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<PermissionPolicy> UpdatePermissionPolicyAsync(PermissionPolicy policy, CancellationToken ct = default)
    {
        policy.UpdatedAt = DateTime.UtcNow;
        _db.PermissionPolicies.Update(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<bool> DeletePermissionPolicyAsync(int id, CancellationToken ct = default)
    {
        var policy = await _db.PermissionPolicies.FindAsync([id], ct);
        if (policy is null)
        {
            return false;
        }

        _db.PermissionPolicies.Remove(policy);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
