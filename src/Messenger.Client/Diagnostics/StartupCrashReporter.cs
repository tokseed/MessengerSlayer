using System.Runtime.InteropServices;
using System.Text;

namespace Messenger.Client.Diagnostics;

public static class StartupCrashReporter
{
    private const uint MessageBoxIconError =
        0x00000010;

    private const uint MessageBoxOk =
        0x00000000;

    public static void InstallGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException +=
            (_, eventArgs) =>
            {
                if (eventArgs.ExceptionObject
                    is Exception exception)
                {
                    Report(
                        "Unhandled application error",
                        exception);
                }
            };

        TaskScheduler.UnobservedTaskException +=
            (_, eventArgs) =>
            {
                Report(
                    "Unobserved task error",
                    eventArgs.Exception);

                eventArgs.SetObserved();
            };
    }

    public static void Report(
        string title,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(
            exception);

        string logPath =
            GetLogPath();

        string message =
            BuildMessage(
                exception,
                logPath);

        try
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(logPath)!);

            File.AppendAllText(
                logPath,
                BuildLogEntry(
                    title,
                    exception),
                Encoding.UTF8);
        }
        catch
        {
            // Reporting must never replace the original startup exception.
        }

        if (OperatingSystem.IsWindows())
        {
            try
            {
                MessageBoxW(
                    IntPtr.Zero,
                    message,
                    $"MessengerSlayer — {title}",
                    MessageBoxOk |
                    MessageBoxIconError);

                return;
            }
            catch
            {
                // Fall through to stderr for non-standard Windows hosts.
            }
        }

        try
        {
            Console.Error.WriteLine(
                message);
        }
        catch
        {
        }
    }

    public static string GetLogPath()
    {
        string localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(
                localAppData))
        {
            localAppData =
                AppContext.BaseDirectory;
        }

        return Path.Combine(
            localAppData,
            "MessengerSlayer",
            "client-crash.log");
    }

    private static string BuildMessage(
        Exception exception,
        string logPath)
    {
        Exception root =
            GetRootException(
                exception);

        return
            "Клиент не смог запуститься.\n\n" +
            $"{root.GetType().Name}: {root.Message}\n\n" +
            "Полный лог сохранён сюда:\n" +
            logPath +
            "\n\n" +
            "Пришли мне client-crash.log, если ошибка повторится.";
    }

    private static string BuildLogEntry(
        string title,
        Exception exception)
    {
        return
            Environment.NewLine +
            "==================================================" +
            Environment.NewLine +
            $"UTC: {DateTime.UtcNow:O}" +
            Environment.NewLine +
            $"Title: {title}" +
            Environment.NewLine +
            $"BaseDirectory: {AppContext.BaseDirectory}" +
            Environment.NewLine +
            $"OS: {RuntimeInformation.OSDescription}" +
            Environment.NewLine +
            $"Framework: {RuntimeInformation.FrameworkDescription}" +
            Environment.NewLine +
            Environment.NewLine +
            exception +
            Environment.NewLine;
    }

    private static Exception GetRootException(
        Exception exception)
    {
        Exception current =
            exception;

        while (current.InnerException != null)
        {
            current =
                current.InnerException;
        }

        return current;
    }

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern int MessageBoxW(
        IntPtr hWnd,
        string text,
        string caption,
        uint type);
}
