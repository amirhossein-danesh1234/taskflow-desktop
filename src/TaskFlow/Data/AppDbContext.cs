using DoktorTasks.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace DoktorTasks.Data;

public class AppDbContext : DbContext
{
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<UserProgress> Progress => Set<UserProgress>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<HistoryEntry> History => Set<HistoryEntry>();
    public DbSet<Achievement> Achievements => Set<Achievement>();

    private readonly string _dbPath;

    public AppDbContext()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DoktorTasks");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "tasks.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>()
            .HasMany<TaskItem>()
            .WithOne(t => t.User!)
            .HasForeignKey(t => t.UserId);

        modelBuilder.Entity<UserProfile>()
            .HasOne(u => u.Progress)
            .WithOne(p => p.User!)
            .HasForeignKey<UserProgress>(p => p.UserId);

        modelBuilder.Entity<TaskItem>()
            .Property(t => t.Status)
            .HasConversion<int>();

        modelBuilder.Entity<TaskItem>()
            .Property(t => t.Recurrence)
            .HasConversion<int>();
    }
}
