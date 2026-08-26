namespace DoktorTasks.Models;

public class OptionItem<T>
{
    public string Title { get; set; } = string.Empty;
    public T Value { get; set; } = default!;
}
