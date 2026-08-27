using System.Text.Json;

namespace Messenger.Client.Configuration;

public sealed class ClientEndpointOptions
{
    public string Host { get; init; } =
        "localhost";

    public int Port { get; init; } =
        5000;

    public bool UseTls { get; init; } =
        true;

    public string PinnedCertificatePath { get; init; } =
        "Certs/server.crt";

    public int ChatSyncIntervalMilliseconds { get; init; } =
        1500;

    public static ClientEndpointOptions Load()
    {
        string path =
            Path.Combine(
                AppContext.BaseDirectory,
                "clientsettings.json");

        if (!File.Exists(path))
        {
            return new ClientEndpointOptions();
        }

        string json =
            File.ReadAllText(path);

        ClientEndpointOptions options =
            JsonSerializer.Deserialize<ClientEndpointOptions>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
            ?? new ClientEndpointOptions();

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new InvalidOperationException(
                "Client host cannot be empty.");
        }

        if (options.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "Client port must be between 1 and 65535.");
        }

        if (options.ChatSyncIntervalMilliseconds < 500)
        {
            throw new InvalidOperationException(
                "Chat sync interval must be at least 500 ms.");
        }

        return options;
    }

    public string GetPinnedCertificatePath()
    {
        if (Path.IsPathRooted(PinnedCertificatePath))
        {
            return PinnedCertificatePath;
        }

        return Path.Combine(
            AppContext.BaseDirectory,
            PinnedCertificatePath);
    }
}
