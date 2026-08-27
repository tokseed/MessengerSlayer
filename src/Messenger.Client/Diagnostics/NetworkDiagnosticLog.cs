using System.Text;

namespace Messenger.Client.Diagnostics;

public static class NetworkDiagnosticLog
{
    private static readonly object Gate =
        new();

    public static string LogPath
    {
        get
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            return Path.Combine(
                localAppData,
                "MessengerSlayer",
                "client-network.log");
        }
    }

    public static void Write(
        string message)
    {
        try
        {
            lock (Gate)
            {
                string? directory =
                    Path.GetDirectoryName(
                        LogPath);

                if (!string.IsNullOrWhiteSpace(
                        directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                File.AppendAllText(
                    LogPath,
                    $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}",
                    Encoding.UTF8);
            }
        }
        catch
        {
            // Diagnostics must never break networking.
        }
    }

    public static void WriteException(
        string operation,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(
            exception);

        Write(
            $"{operation}: {exception}");
    }
}
