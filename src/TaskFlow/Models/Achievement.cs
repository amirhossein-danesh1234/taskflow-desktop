using System.ComponentModel.DataAnnotations;

namespace DoktorTasks.Models;

public class Achievement
{
    [Key]
    public int Id { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int TargetValue { get; set; }

    public DateTime? UnlockedDate { get; set; }

    public bool IsUnlocked => UnlockedDate.HasValue;
}
