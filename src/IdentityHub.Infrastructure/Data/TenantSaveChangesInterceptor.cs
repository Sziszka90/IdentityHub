using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace IdentityHub.Infrastructure.Data;

/// <summary>
/// Ensures tenant-owned entities are stamped with the current tenant before EF saves them.
/// </summary>
public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContextService _tenantContextService;
    private readonly TenantConfigurationOptions _tenantOptions;

    public TenantSaveChangesInterceptor(
        ITenantContextService tenantContextService,
        IOptions<TenantConfigurationOptions> tenantOptions)
    {
        _tenantContextService = tenantContextService;
        _tenantOptions = tenantOptions.Value;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTenantScope(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantScope(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenantScope(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var currentTenantId = _tenantContextService.GetTenantContext().TenantId;
        var effectiveTenantId = string.IsNullOrWhiteSpace(currentTenantId)
            ? _tenantOptions.SeedTenantIds.FirstOrDefault()
            : currentTenantId;

        foreach (var entry in context.ChangeTracker.Entries<ITenantOwnedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(entry.Entity.TenantId))
                {
                    if (string.IsNullOrWhiteSpace(effectiveTenantId))
                    {
                        throw new InvalidOperationException("Tenant ID is required when saving tenant-owned entities.");
                    }

                    entry.Entity.TenantId = effectiveTenantId;
                }

                continue;
            }

            if (entry.State == EntityState.Modified
                && !string.IsNullOrWhiteSpace(effectiveTenantId)
                && !string.Equals(entry.Entity.TenantId, effectiveTenantId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Cross-tenant updates are not allowed.");
            }
        }
    }
}
