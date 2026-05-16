using IdentityHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for authorization configuration persistence.
/// </summary>
public class IdentityHubDbContext : DbContext
{
    public IdentityHubDbContext(DbContextOptions<IdentityHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<GroupRoleMapping> GroupRoleMappings => Set<GroupRoleMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Role ──
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Description).HasMaxLength(500);
        });

        // ── Permission ──
        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(p => p.Name).IsUnique();
            e.Property(p => p.Description).HasMaxLength(500);
        });

        // ── RolePermission (many-to-many) ──
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermissions");
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });

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
            e.Property(g => g.GroupName).HasMaxLength(200).IsRequired();
            e.HasIndex(g => g.GroupName).IsUnique();

            e.HasOne(g => g.Role)
                .WithMany(r => r.GroupRoleMappings)
                .HasForeignKey(g => g.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
