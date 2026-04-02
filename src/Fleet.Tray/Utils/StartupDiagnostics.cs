using System.IO;
using System.Text;
using System.Windows;
using Application = System.Windows.Application;

namespace Fleet.Tray.Utils;

internal static class StartupDiagnostics
{
    private static readonly object Sync = new();
    private static bool _initialized;

    private static string LogFilePath
    {
        get
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Fleet",
                "Logs");
            Directory.CreateDirectory(logDir);
            return Path.Combine(logDir, "startup.log");
        }
    }

    public static void InitializeGlobalExceptionLogging()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                Error("Unhandled AppDomain exception.", ex);
                return;
            }

            Info($"Unhandled AppDomain exception object: {args.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Error("Unobserved task exception.", args.Exception);
            args.SetObserved();
        };

        if (Application.Current is not null)
        {
            Application.Current.DispatcherUnhandledException += (_, args) =>
            {
                Error("Unhandled dispatcher exception.", args.Exception);
                args.Handled = false;
            };
        }

        Info($"Diagnostics initialized. Startup log: {LogFilePath}");
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

        Console.WriteLine(line);
    }
}
