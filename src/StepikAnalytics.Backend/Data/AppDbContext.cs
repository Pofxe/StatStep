using Microsoft.EntityFrameworkCore;
using StepikAnalytics.Backend.Models;

namespace StepikAnalytics.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();
    public DbSet<MetricsDaily> MetricsDaily => Set<MetricsDaily>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Course>(e =>
        {
            e.HasIndex(c => c.StepikCourseId).IsUnique();
            e.HasMany(c => c.SyncRuns).WithOne(s => s.Course).HasForeignKey(s => s.CourseId);
            e.HasMany(c => c.DailyMetrics).WithOne(m => m.Course).HasForeignKey(m => m.CourseId);
        });

        modelBuilder.Entity<SyncRun>(e =>
        {
            e.HasIndex(s => new { s.CourseId, s.StartedAt });
        });

        modelBuilder.Entity<MetricsDaily>(e =>
        {
            e.HasIndex(m => m.Date);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });
    }
}
