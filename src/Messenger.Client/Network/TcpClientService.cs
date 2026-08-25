using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Messenger.Shared.Network;
using Messenger.Shared.Packets;

namespace Messenger.Client.Network;

public sealed class TcpClientService : IDisposable
{
    private readonly System.Net.Sockets.TcpClient _client;
    private SslStream? _sslStream;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _client.Connected;

    public event Action<Packet>? OnPacketReceived;
    public event Action? OnDisconnected;

    public TcpClientService()
    {
        _client = new System.Net.Sockets.TcpClient();
    }

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(host, port, cancellationToken);
        var stream = _client.GetStream();

        _sslStream = new SslStream(stream, false, ValidateServerCertificate);
        await _sslStream.AuthenticateAsClientAsync(
            new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck
            },
            cancellationToken);

        Console.WriteLine("TLS handshake OK.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => ListenAsync(_cts.Token), _cts.Token);
    }

    private static bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        // For development: accept self-signed certificates
        Console.WriteLine($"Certificate warning: {sslPolicyErrors}");
        return true;
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_client.Connected && !cancellationToken.IsCancellationRequested && _sslStream != null)
            {
                var packet = await PacketSerializer.ReceiveAsync(_sslStream, cancellationToken);
                if (packet == null)
                    break;

                OnPacketReceived?.Invoke(packet);
            }
        }
        catch (OperationCanceledException) { }
        catch { }

        OnDisconnected?.Invoke();
    }

    public async Task SendAsync(Packet packet, CancellationToken cancellationToken = default)
    {
        if (_sslStream == null)
            throw new InvalidOperationException("Not connected");

        await PacketSerializer.SendAsync(_sslStream, packet, cancellationToken);
    }

    public async Task<Packet> SendAndWaitAsync(Packet packet, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<Packet>();

        void Handler(Packet response)
        {
            tcs.TrySetResult(response);
            OnPacketReceived -= Handler;
        }

        OnPacketReceived += Handler;

        await SendAsync(packet, cancellationToken);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        timeout.Token.Register(() =>
        {
            tcs.TrySetCanceled();
            OnPacketReceived -= Handler;
        });

        return await tcs.Task;
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _sslStream?.Dispose();
        _client.Close();
    }

    public void Dispose()
    {
        Disconnect();
        _client.Dispose();
    }
}
