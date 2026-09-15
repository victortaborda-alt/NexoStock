using NexoStock.Infrastructure.Identity;
using NexoStock.Domain.Articles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NexoStock.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        builder.Entity<IdentityRole>().ToTable("Roles");

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.RoleName).HasMaxLength(256).IsRequired();
            entity.Property(permission => permission.Permission).HasMaxLength(100).IsRequired();
            entity.HasIndex(permission => new { permission.RoleName, permission.Permission }).IsUnique();
        });

        builder.Entity<Article>(entity =>
        {
            entity.HasKey(article => article.Id);
            entity.Property(article => article.Code).HasMaxLength(50).IsRequired();
            entity.Property(article => article.Name).HasMaxLength(150).IsRequired();
            entity.Property(article => article.Description).HasMaxLength(500).IsRequired();
            entity.Property(article => article.CreatedAtUtc).IsRequired();
            entity.HasIndex(article => article.Code).IsUnique();
        });
    }
}
