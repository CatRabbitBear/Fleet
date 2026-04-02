using System.Text;

namespace Fleet.Blazor.Utilities;

internal static class ChatDiagnostics
{
    private static readonly object Sync = new();

    private static string LogFilePath
    {
        get
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Fleet",
                "Logs");
            Directory.CreateDirectory(logDir);
            return Path.Combine(logDir, "chatbox.log");
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message, null);
    }

    public static void Error(string message, Exception exception)
    {
        Write("ERROR", message, exception);
    }

    private static void Write(string level, string message, Exception? exception)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var builder = new StringBuilder()
            .Append(timestamp)
            .Append(" [")
            .Append(level)
            .Append("] ")
            .Append(message);

        if (exception is not null)
        {
            builder.AppendLine();
            builder.Append(exception);
        }

        var line = builder.ToString();
        lock (Sync)
        {
            File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
        }
    }
}
