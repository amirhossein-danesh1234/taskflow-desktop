using System.Windows;
using DoktorTasks.Services;
using WpfApplication = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace DoktorTasks;

public partial class App : WpfApplication
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            LogService.Error("Unhandled UI exception", e.Exception);
            MessageBox.Show("Unexpected error occurred. Details were written to the log file.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
