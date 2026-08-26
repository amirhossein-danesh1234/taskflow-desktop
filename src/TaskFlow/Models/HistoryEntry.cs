using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoktorTasks.Models;

public class HistoryEntry
{
    [Key]
    public int Id { get; set; }

    public int TaskId { get; set; }

    public DateTime CompletionDate { get; set; } = DateTime.UtcNow;

    public int XpGained { get; set; }

    [ForeignKey(nameof(TaskId))]
    public TaskItem? Task { get; set; }
}
