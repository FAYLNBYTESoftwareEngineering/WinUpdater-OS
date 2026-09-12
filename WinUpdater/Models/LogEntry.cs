namespace WinUpdater.Models;

public enum LogLevel
{
    Info,
    Run,
    Ok,
    Warn,
    Stop,
    Error
}

public class LogEntry
{
    private DateTime Timestamp { get; set; } = DateTime.Now;
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;

    public string LevelTag => Level switch
    {
        LogLevel.Info => "[INFO]",
        LogLevel.Run => "[RUN]",
        LogLevel.Ok => "[OK]",
        LogLevel.Warn => "[WARN]",
        LogLevel.Stop => "[STOP]",
        LogLevel.Error => "[ERR]",
        _ => "[?]"
    };

    public string TimeString => Timestamp.ToString("HH:mm:ss");
}