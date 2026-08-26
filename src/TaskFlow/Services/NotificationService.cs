using DoktorTasks.Models;
using System.Windows.Forms;

namespace DoktorTasks.Services;

public interface INotificationService
{
    Task SendTaskReminderAsync(TaskItem task);
    Task SendInfoAsync(string title, string message);
}

/// <summary>
/// Simple balloon notification using <see cref="NotifyIcon"/>. The Windows SDK package is referenced so that
/// real toast notifications can be enabled in the future without touching the data layer.
/// </summary>
public class NotificationService : INotificationService
{
    public Task SendTaskReminderAsync(TaskItem task)
    {
        var title = "Task reminder";
        var body = $"{task.Title} - Due: {(task.DueDate?.ToLocalTime().ToString("yyyy/MM/dd HH:mm") ?? "unscheduled")}";
        return ShowBalloonAsync(title, body);
    }

    public Task SendInfoAsync(string title, string message) => ShowBalloonAsync(title, message);

    private Task ShowBalloonAsync(string title, string message)
    {
        return Task.Run(async () =>
        {
            try
            {
                using var icon = new NotifyIcon
                {
                    Visible = true,
                    Icon = System.Drawing.SystemIcons.Information,
                    BalloonTipTitle = title,
                    BalloonTipText = message
                };
                icon.ShowBalloonTip(4000);
                await Task.Delay(4500);
            }
            catch (Exception ex)
            {
                LogService.Error("Sending balloon notification failed", ex);
            }
        });
    }
}
