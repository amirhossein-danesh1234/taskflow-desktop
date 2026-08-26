using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoktorTasks.Models;

public class TaskItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

    public int XpReward { get; set; } = 10;

    public int XpPenalty { get; set; } = 0;

    public int Bonus { get; set; } = 0;

    [MaxLength(200)]
    public string? Category { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserProfile? User { get; set; }

    [NotMapped]
    public string StatusTitle => Status switch
    {
        TaskStatus.Pending => "Pending",
        TaskStatus.InProgress => "In progress",
        TaskStatus.Completed => "Completed",
        TaskStatus.Archived => "Archived",
        _ => Status.ToString()
    };

    [NotMapped]
    public string RecurrenceTitle => Recurrence switch
    {
        RecurrenceType.None => "None",
        RecurrenceType.Daily => "Daily",
        RecurrenceType.Weekly => "Weekly",
        RecurrenceType.Monthly => "Monthly",
        _ => Recurrence.ToString()
    };
}
