using Microsoft.EntityFrameworkCore;
using Nex.Api.Data.Entities;

namespace Nex.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamInvite> TeamInvites => Set<TeamInvite>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<UploadedImage> UploadedImages => Set<UploadedImage>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).IsRequired().HasMaxLength(320);
            e.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Organization).HasMaxLength(200);
        });

        mb.Entity<Team>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Slug).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<TeamMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique();
            e.HasOne(x => x.Team).WithMany(t => t.Members).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.TeamMemberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<TeamInvite>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Email).IsRequired().HasMaxLength(320);
            e.Property(x => x.Token).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Team).WithMany(t => t.Invites).HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Article>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.Slug).IsRequired().HasMaxLength(300);
            e.Property(x => x.Category).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne(x => x.Author).WithMany(u => u.Articles).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<UploadedImage>(e =>
        {
            e.HasKey(x => x.Id);
        });

        mb.Entity<UserSetting>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Key }).IsUnique();
            e.Property(x => x.Key).IsRequired().HasMaxLength(100);
        });
    }
}
