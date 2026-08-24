using System;

namespace Messenger.Client.Configuration;

public sealed class ClientEndpointOptions
{
    public ClientEndpointOptions(
        string host,
        int port,
        bool useTls)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException(
                "Host cannot be null, empty or whitespace.",
                nameof(host));
        }

        if (port < 1 ||
            port > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(port));
        }

        Host = host.Trim();
        Port = port;
        UseTls = useTls;
    }

    public string Host { get; }

    public int Port { get; }

    public bool UseTls { get; }

    public static ClientEndpointOptions
        CreateDevelopmentDefault()
    {
        return new ClientEndpointOptions(
            host: "127.0.0.1",
            port: 5050,
            useTls: true);
    }
}
