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
    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<PullRequest> PullRequests => Set<PullRequest>();
    public DbSet<IssueComment> IssueComments => Set<IssueComment>();
    public DbSet<SshKey> SshKeys => Set<SshKey>();

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

            entity.HasMany(u => u.CreatedIssues)
                  .WithOne(i => i.Creator)
                  .HasForeignKey(i => i.CreatorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.AssignedIssues)
                  .WithOne(i => i.Assignee)
                  .HasForeignKey(i => i.AssigneeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Repository>(entity =>
        {
            entity.Property(r => r.Name).HasMaxLength(256);
            entity.Property(r => r.Status).HasMaxLength(50);

            entity.HasMany(r => r.BoardColumns)
                  .WithOne(bc => bc.Repository)
                  .HasForeignKey(bc => bc.RepositoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Issues)
                  .WithOne(i => i.Repository)
                  .HasForeignKey(i => i.RepositoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BoardColumn>(entity =>
        {
            entity.Property(bc => bc.Name).HasMaxLength(100);

            entity.HasMany(bc => bc.Issues)
                  .WithOne(i => i.Column)
                  .HasForeignKey(i => i.ColumnId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.Property(i => i.Title).HasMaxLength(500);

            // Composite unique constraint: LocalId per Repository
            entity.HasIndex(i => new { i.RepositoryId, i.LocalId }).IsUnique();

            entity.HasOne(i => i.PullRequest)
                  .WithOne(pr => pr.Issue)
                  .HasForeignKey<PullRequest>(pr => pr.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(i => i.Comments)
                  .WithOne(c => c.Issue)
                  .HasForeignKey(c => c.IssueId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IssueComment>(entity =>
        {
            entity.HasOne(c => c.Author)
                  .WithMany()
                  .HasForeignKey(c => c.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SshKey>(entity =>
        {
            entity.HasIndex(k => k.Fingerprint).IsUnique();
            entity.Property(k => k.Label).HasMaxLength(256);
            entity.Property(k => k.PublicKey).HasColumnType("text");
            entity.Property(k => k.Fingerprint).HasMaxLength(256);

            entity.HasOne(k => k.User)
                  .WithMany(u => u.SshKeys)
                  .HasForeignKey(k => k.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
