using Microsoft.Win32;

namespace DoktorTasks.Services;

public class AutoStartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DoktorTasks";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            LogService.Error("Failed to read auto-start settings", ex);
            return false;
        }
    }

    public Task SetAutoStartAsync(bool enable)
    {
        return Task.Run(() =>
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true) ??
                                Registry.CurrentUser.CreateSubKey(RunKey);

                if (enable)
                {
                    var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(exePath))
                    {
                        key?.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key?.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to update auto-start settings", ex);
            }
        });
    }
}
