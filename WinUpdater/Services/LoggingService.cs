using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using WinUpdater.Models;

namespace WinUpdater.Services;

public class LoggingService
{
    private readonly string logFilePath;

    private ObservableCollection<LogEntry> Entries { get; } = new();

    public LoggingService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinUpdater");
        Directory.CreateDirectory(folder);
        logFilePath = Path.Combine(folder, "UpdateLog.txt");
    }

    public void Add(LogLevel level, string message)
    {
        var entry = new LogEntry { Level = level, Message = message };
        Entries.Add(entry);
        AppendToFile(entry);
    }

    public void Clear() => Entries.Clear();

    public string LogFilePath => logFilePath;

    public void ExportToDesktop(string? extraContent = null)
    {
        var fileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"WinUpdater_Export_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

        var sb = new StringBuilder();
        foreach (var e in Entries)
            sb.AppendLine($"{e.TimeString}  {e.LevelTag,-8}  {e.Message}");

        if (!string.IsNullOrEmpty(extraContent))
        {
            sb.AppendLine();
            sb.AppendLine("--- Console Output ---");
            sb.AppendLine(extraContent);
        }

        File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
    }

    private void AppendToFile(LogEntry entry)
    {
        try
        {
            File.AppendAllText(
                logFilePath,
                $"{entry.TimeString}  {entry.LevelTag,-8}  {entry.Message}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch
        {
            /* niemals crashen wegen Logging */
        }
    }
}