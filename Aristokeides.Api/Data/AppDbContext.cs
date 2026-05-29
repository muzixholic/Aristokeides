using Aristokeides.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Data;

/// <summary>
/// 애플리케이션 데이터베이스 컨텍스트. PostgreSQL에 연결.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Repository> Repositories => Set<Repository>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.Username).HasMaxLength(256);
            entity.Property(u => u.Role).HasMaxLength(50);
            
            entity.HasMany(u => u.Repositories)
                  .WithOne(r => r.Owner)
                  .HasForeignKey(r => r.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Repository>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(256);
            entity.Property(r => r.Status).HasMaxLength(50);
        });
    }
}
