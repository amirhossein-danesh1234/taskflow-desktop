using System.Collections.ObjectModel;
using DoktorTasks.Models;
using DoktorTasks.Services;
using TaskStatusEnum = DoktorTasks.Models.TaskStatus;

namespace DoktorTasks.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IDataService _dataService;
    private readonly INotificationService _notificationService;
    private readonly AutoStartService _autoStartService;

    private UserProgress _progress = new();
    private TaskItem? _selectedTask;
    private string _statusMessage = string.Empty;
    private bool _autoStartEnabled;
    private bool _isBusy;

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<Achievement> Achievements { get; } = new();
    public ObservableCollection<HistoryEntry> HistoryEntries { get; } = new();

    public ObservableCollection<OptionItem<RecurrenceType>> RecurrenceOptions { get; } = new()
    {
        new OptionItem<RecurrenceType>{ Title = "No recurrence", Value = RecurrenceType.None },
        new OptionItem<RecurrenceType>{ Title = "Daily", Value = RecurrenceType.Daily },
        new OptionItem<RecurrenceType>{ Title = "Weekly", Value = RecurrenceType.Weekly },
        new OptionItem<RecurrenceType>{ Title = "Monthly", Value = RecurrenceType.Monthly }
    };

    public ObservableCollection<OptionItem<TaskStatusEnum>> StatusOptions { get; } = new()
    {
        new OptionItem<TaskStatusEnum>{ Title = "Pending", Value = TaskStatusEnum.Pending },
        new OptionItem<TaskStatusEnum>{ Title = "In progress", Value = TaskStatusEnum.InProgress },
        new OptionItem<TaskStatusEnum>{ Title = "Completed", Value = TaskStatusEnum.Completed }
    };

    public UserProgress Progress
    {
        get => _progress;
        set
        {
            SetProperty(ref _progress, value);
            OnPropertyChanged(nameof(CurrentLevelXp));
            OnPropertyChanged(nameof(NextLevelXp));
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public int CurrentLevelXp => Progress.Xp % 100;
    public int NextLevelXp => 100;
    public string ProgressText => $"Level {Progress.Level} | XP: {Progress.Xp}";

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            SetProperty(ref _selectedTask, value);
            RaiseCommandStates();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool AutoStartEnabled
    {
        get => _autoStartEnabled;
        set => SetProperty(ref _autoStartEnabled, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            SetProperty(ref _isBusy, value);
            RaiseCommandStates();
        }
    }

    // Fields for the new task form
    public string NewTitle { get; set; } = string.Empty;
    public string? NewDescription { get; set; }
    public string? NewCategory { get; set; }
    public DateTime? NewDueDate { get; set; } = DateTime.Today.AddDays(1);
    public RecurrenceType NewRecurrence { get; set; } = RecurrenceType.None;
    public int NewXpReward { get; set; } = 10;
    public int NewXpPenalty { get; set; } = 0;
    public int NewBonus { get; set; } = 0;
    public TaskStatusEnum NewStatus { get; set; } = TaskStatusEnum.Pending;

    public AsyncRelayCommand InitializeCommand { get; }
    public AsyncRelayCommand AddTaskCommand { get; }
    public AsyncRelayCommand CompleteTaskCommand { get; }
    public AsyncRelayCommand SyncCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand ToggleAutoStartCommand { get; }

    public MainViewModel(IDataService dataService, INotificationService notificationService, AutoStartService autoStartService)
    {
        _dataService = dataService;
        _notificationService = notificationService;
        _autoStartService = autoStartService;

        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        AddTaskCommand = new AsyncRelayCommand(AddTaskAsync, () => !IsBusy);
        CompleteTaskCommand = new AsyncRelayCommand(CompleteTaskAsync, () => SelectedTask != null && !IsBusy);
        SyncCommand = new AsyncRelayCommand(SyncAsync, () => !IsBusy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        ToggleAutoStartCommand = new AsyncRelayCommand(ToggleAutoStartAsync);
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading data...";

            await _dataService.InitializeAsync();
            await RefreshAsync();

            AutoStartEnabled = _autoStartService.IsEnabled();
            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to load data.";
            LogService.Error("InitializeAsync failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseCommandStates()
    {
        AddTaskCommand.RaiseCanExecuteChanged();
        CompleteTaskCommand.RaiseCanExecuteChanged();
        SyncCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
    }

    private async Task RefreshAsync()
    {
        Tasks.Clear();
        Achievements.Clear();
        HistoryEntries.Clear();

        var tasks = await _dataService.GetTasksAsync();
        foreach (var item in tasks)
            Tasks.Add(item);

        var achievements = await _dataService.GetAchievementsAsync();
        foreach (var a in achievements)
            Achievements.Add(a);

        var history = await _dataService.GetHistoryAsync();
        foreach (var h in history)
            HistoryEntries.Add(h);

        Progress = await _dataService.GetProgressAsync();
    }

    private async Task AddTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTitle))
        {
            StatusMessage = "Enter a title before saving.";
            return;
        }

        try
        {
            IsBusy = true;
            var newTask = new TaskItem
            {
                Title = NewTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(NewDescription) ? null : NewDescription.Trim(),
                Category = string.IsNullOrWhiteSpace(NewCategory) ? null : NewCategory.Trim(),
                DueDate = NewDueDate,
                Recurrence = NewRecurrence,
                XpReward = Math.Max(0, NewXpReward),
                XpPenalty = Math.Max(0, NewXpPenalty),
                Bonus = Math.Max(0, NewBonus),
                Status = NewStatus
            };

            var saved = await _dataService.AddTaskAsync(newTask);
            Tasks.Add(saved);
            StatusMessage = "Task saved.";

            if (saved.DueDate.HasValue)
            {
                _ = _notificationService.SendTaskReminderAsync(saved);
            }

            ClearNewTaskForm();
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not save the task.";
            LogService.Error("AddTaskAsync failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearNewTaskForm()
    {
        NewTitle = string.Empty;
        NewDescription = null;
        NewCategory = null;
        NewDueDate = DateTime.Today.AddDays(1);
        NewRecurrence = RecurrenceType.None;
        NewXpReward = 10;
        NewXpPenalty = 0;
        NewBonus = 0;
        NewStatus = TaskStatusEnum.Pending;

        OnPropertyChanged(nameof(NewTitle));
        OnPropertyChanged(nameof(NewDescription));
        OnPropertyChanged(nameof(NewCategory));
        OnPropertyChanged(nameof(NewDueDate));
        OnPropertyChanged(nameof(NewRecurrence));
        OnPropertyChanged(nameof(NewXpReward));
        OnPropertyChanged(nameof(NewXpPenalty));
        OnPropertyChanged(nameof(NewBonus));
        OnPropertyChanged(nameof(NewStatus));
    }

    private async Task CompleteTaskAsync()
    {
        if (SelectedTask == null)
        {
            StatusMessage = "Select a task first.";
            return;
        }

        try
        {
            IsBusy = true;
            await _dataService.CompleteTaskAsync(SelectedTask);
            await RefreshAsync();
            StatusMessage = "Task completed.";
            await _notificationService.SendInfoAsync("Great job!", "Task completed successfully.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not complete the task.";
            LogService.Error("CompleteTaskAsync failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task SyncAsync()
    {
        StatusMessage = "Sync is local-only for now.";
        return Task.CompletedTask;
    }

    private async Task ToggleAutoStartAsync()
    {
        try
        {
            await _autoStartService.SetAutoStartAsync(AutoStartEnabled);
            StatusMessage = AutoStartEnabled ? "Auto-start enabled." : "Auto-start disabled.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to update auto-start settings.";
            LogService.Error("ToggleAutoStartAsync failed", ex);
        }
    }

    public async Task DeleteSelectedTaskAsync()
    {
        if (SelectedTask == null)
        {
            StatusMessage = "Select a task first.";
            return;
        }

        try
        {
            IsBusy = true;
            await _dataService.DeleteTaskAsync(SelectedTask.Id);
            await RefreshAsync();
            StatusMessage = "Task deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not delete the task.";
            LogService.Error("DeleteSelectedTaskAsync failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
