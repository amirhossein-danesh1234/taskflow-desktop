using System.ComponentModel.DataAnnotations;

namespace DoktorTasks.Models;

public class UserProfile
{
    [Key]
    public int Id { get; set; }

    [MaxLength(200)]
    public string DisplayName { get; set; } = "Primary User";

    public UserProgress? Progress { get; set; }
}
