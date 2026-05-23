using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for authorization configuration persistence.
/// </summary>
public class IdentityHubDbContext : DbContext
{
    private readonly ITenantContextService? _tenantContextService;

    public IdentityHubDbContext(DbContextOptions<IdentityHubDbContext> options)
        : this(options, null)
    {
    }

    public IdentityHubDbContext(
        DbContextOptions<IdentityHubDbContext> options,
        ITenantContextService? tenantContextService)
        : base(options)
    {
        _tenantContextService = tenantContextService;
    }

    private string? CurrentTenantId => _tenantContextService?.GetTenantContext().TenantId;

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<GroupRoleMapping> GroupRoleMappings => Set<GroupRoleMapping>();
    public DbSet<UserTenantMapping> UserTenantMappings => Set<UserTenantMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Role ──
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).HasMaxLength(100).IsRequired();
            e.Property(r => r.TenantId).HasMaxLength(100).IsRequired();
            e.HasIndex(r => new { r.TenantId, r.Name }).IsUnique();
            e.Property(r => r.Description).HasMaxLength(500);
            e.HasQueryFilter(r => string.IsNullOrEmpty(CurrentTenantId) || r.TenantId == CurrentTenantId);
        });

        // ── Permission ──
        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.TenantId).HasMaxLength(100).IsRequired();
            e.HasIndex(p => new { p.TenantId, p.Name }).IsUnique();
            e.Property(p => p.Description).HasMaxLength(500);
            e.HasQueryFilter(p => string.IsNullOrEmpty(CurrentTenantId) || p.TenantId == CurrentTenantId);
        });

        // ── RolePermission (many-to-many) ──
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermissions");
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            e.HasQueryFilter(rp => string.IsNullOrEmpty(CurrentTenantId)
                || (rp.Role.TenantId == CurrentTenantId && rp.Permission.TenantId == CurrentTenantId));

            e.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── GroupRoleMapping ──
        modelBuilder.Entity<GroupRoleMapping>(e =>
        {
            e.ToTable("GroupRoleMappings");
            e.HasKey(g => g.Id);
            e.Property(g => g.GroupId).IsRequired();
            e.Property(g => g.TenantId).HasMaxLength(100).IsRequired();
            e.HasIndex(g => new { g.TenantId, g.GroupId }).IsUnique();
            e.HasQueryFilter(g => string.IsNullOrEmpty(CurrentTenantId) || g.TenantId == CurrentTenantId);

            e.HasOne(g => g.Role)
                .WithMany(r => r.GroupRoleMappings)
                .HasForeignKey(g => g.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── UserTenantMapping ──
        modelBuilder.Entity<UserTenantMapping>(e =>
        {
            e.ToTable("UserTenantMappings");
            e.HasKey(u => u.Id);
            e.Property(u => u.UserId).HasMaxLength(100).IsRequired();
            e.Property(u => u.TenantId).HasMaxLength(100).IsRequired();
            e.HasIndex(u => new { u.TenantId, u.UserId }).IsUnique();
            e.HasQueryFilter(u => string.IsNullOrEmpty(CurrentTenantId) || u.TenantId == CurrentTenantId);
        });

    }
}
