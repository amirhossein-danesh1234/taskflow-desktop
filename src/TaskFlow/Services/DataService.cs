using DoktorTasks.Data;
using DoktorTasks.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatusEnum = DoktorTasks.Models.TaskStatus;

namespace DoktorTasks.Services;

public interface IDataService
{
    Task InitializeAsync();
    Task<List<TaskItem>> GetTasksAsync();
    Task<List<HistoryEntry>> GetHistoryAsync();
    Task<List<Achievement>> GetAchievementsAsync();
    Task<UserProgress> GetProgressAsync();
    Task<TaskItem> AddTaskAsync(TaskItem task);
    Task UpdateTaskAsync(TaskItem task);
    Task CompleteTaskAsync(TaskItem task);
    Task DeleteTaskAsync(int taskId);
}

public class DataService : IDataService
{
    private readonly AppDbContext _context;
    private const int DefaultUserId = 1;
    private const int XpPerLevel = 100;

    public DataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();

            if (!await _context.Users.AnyAsync())
            {
                var user = new UserProfile { Id = DefaultUserId, DisplayName = "Primary User" };
                var progress = new UserProgress { UserId = DefaultUserId, Level = 1, Xp = 0, TotalXp = 0 };
                _context.Users.Add(user);
                _context.Progress.Add(progress);
                await _context.SaveChangesAsync();
            }

            if (!await _context.Achievements.AnyAsync())
            {
                _context.Achievements.AddRange(
                    new Achievement { Description = "Earn 100 XP", TargetValue = 100 },
                    new Achievement { Description = "Earn 500 XP", TargetValue = 500 },
                    new Achievement { Description = "Earn 1,000 XP", TargetValue = 1000 }
                );
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Database initialization failed", ex);
            throw;
        }
    }

    public async Task<List<TaskItem>> GetTasksAsync()
    {
        try
        {
            return await _context.Tasks
                .Where(t => t.UserId == DefaultUserId)
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            LogService.Error("Reading tasks failed", ex);
            return new List<TaskItem>();
        }
    }

    public async Task<List<HistoryEntry>> GetHistoryAsync()
    {
        try
        {
            return await _context.History
                .Include(h => h.Task)
                .OrderByDescending(h => h.CompletionDate)
                .Take(30)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            LogService.Error("Reading history failed", ex);
            return new List<HistoryEntry>();
        }
    }

    public async Task<List<Achievement>> GetAchievementsAsync()
    {
        try
        {
            return await _context.Achievements
                .OrderBy(a => a.TargetValue)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            LogService.Error("Reading achievements failed", ex);
            return new List<Achievement>();
        }
    }

    public async Task<UserProgress> GetProgressAsync()
    {
        var progress = await _context.Progress.FirstOrDefaultAsync(p => p.UserId == DefaultUserId);
        if (progress == null)
        {
            progress = new UserProgress { UserId = DefaultUserId, Level = 1, Xp = 0, TotalXp = 0 };
            _context.Progress.Add(progress);
            await _context.SaveChangesAsync();
        }

        return progress;
    }

    public async Task<TaskItem> AddTaskAsync(TaskItem task)
    {
        try
        {
            task.UserId = DefaultUserId;
            task.CreatedAt = DateTime.UtcNow;
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }
        catch (Exception ex)
        {
            LogService.Error("Creating task failed", ex);
            throw;
        }
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        try
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            LogService.Error("Updating task failed", ex);
            throw;
        }
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        try
        {
            var entity = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
            if (entity != null)
            {
                _context.Tasks.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            LogService.Error("Deleting task failed", ex);
            throw;
        }
    }

    public async Task CompleteTaskAsync(TaskItem task)
    {
        try
        {
            var entity = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id);
            if (entity == null)
            {
                return;
            }
            var progress = await GetProgressAsync();

            var xpChange = entity.XpReward + entity.Bonus - entity.XpPenalty;
            var previousXp = progress.Xp;

            progress.Xp = Math.Max(0, progress.Xp + xpChange);
            if (xpChange > 0)
            {
                progress.TotalXp += xpChange;
            }

            progress.Level = CalculateLevel(progress.Xp);

            entity.CompletedAt = DateTime.UtcNow;

            if (entity.Recurrence == RecurrenceType.None)
            {
                entity.Status = TaskStatusEnum.Completed;
            }
            else
            {
                entity.Status = TaskStatusEnum.Pending;
                entity.CompletedAt = null;
                entity.DueDate = GetNextDueDate(entity);
            }

            var gained = progress.Xp - previousXp;
            _context.History.Add(new HistoryEntry
            {
                TaskId = entity.Id,
                CompletionDate = DateTime.UtcNow,
                XpGained = gained
            });

            await _context.SaveChangesAsync();
            await UpdateAchievementsAsync(progress);
        }
        catch (Exception ex)
        {
            LogService.Error("Completing task failed", ex);
            throw;
        }
    }

    private static DateTime GetNextDueDate(TaskItem task)
    {
        var start = task.DueDate ?? DateTime.Now;
        return task.Recurrence switch
        {
            RecurrenceType.Daily => start.AddDays(1),
            RecurrenceType.Weekly => start.AddDays(7),
            RecurrenceType.Monthly => start.AddMonths(1),
            _ => start
        };
    }

    private static int CalculateLevel(int xp)
    {
        var level = (xp / XpPerLevel) + 1;
        return Math.Max(1, level);
    }

    private async Task UpdateAchievementsAsync(UserProgress progress)
    {
        var achievements = await _context.Achievements.ToListAsync();
        var unlockedAny = false;

        foreach (var achievement in achievements)
        {
            if (!achievement.IsUnlocked && progress.TotalXp >= achievement.TargetValue)
            {
                achievement.UnlockedDate = DateTime.UtcNow;
                unlockedAny = true;
            }
        }

        if (unlockedAny)
        {
            await _context.SaveChangesAsync();
        }
    }
}
