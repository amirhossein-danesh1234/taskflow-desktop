using System.Text;
using System.IO;

namespace DoktorTasks.Services;

public static class LogService
{
    private static readonly string LogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    private static readonly string LogFile = Path.Combine(LogFolder, "app.log");
    private static readonly object FileLock = new();

    static LogService()
    {
        Directory.CreateDirectory(LogFolder);
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", $"{message} {(ex != null ? ex.ToString() : string.Empty)}");

    private static void Write(string level, string message)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            lock (FileLock)
            {
                File.AppendAllText(LogFile, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never crash the application; swallow IO failures.
        }
    }
}
