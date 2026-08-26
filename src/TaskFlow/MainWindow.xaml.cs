using System.Windows;
using DoktorTasks.Data;
using DoktorTasks.Services;
using DoktorTasks.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace DoktorTasks;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var db = new AppDbContext();
        IDataService dataService = new DataService(db);
        INotificationService notificationService = new NotificationService();
        var autoStartService = new AutoStartService();

        _viewModel = new MainViewModel(dataService, notificationService, autoStartService);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTask == null)
        {
            MessageBox.Show("Select a task before deleting.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show("Delete this task?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            await _viewModel.DeleteSelectedTaskAsync();
        }
    }
}
