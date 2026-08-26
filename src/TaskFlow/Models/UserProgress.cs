using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoktorTasks.Models;

public class UserProgress
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Level { get; set; } = 1;

    public int Xp { get; set; } = 0;

    public int TotalXp { get; set; } = 0;

    [ForeignKey(nameof(UserId))]
    public UserProfile? User { get; set; }
}
