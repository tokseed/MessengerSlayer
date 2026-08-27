using Avalonia;
using Messenger.Client.Diagnostics;

namespace Messenger.Client;

internal static class Program
{
    [STAThread]
    public static int Main(
        string[] args)
    {
        StartupCrashReporter.InstallGlobalHandlers();

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(
                    args);

            return 0;
        }
        catch (Exception exception)
        {
            StartupCrashReporter.Report(
                "Startup error",
                exception);

            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
    }
}
