using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Messenger.Server.Services;
using Messenger.Shared.Network;
using Messenger.Shared.Packets;

namespace Messenger.Server.Network;

public sealed class TcpServer
{
    private readonly TcpListener _listener;
    private readonly AuthService _authService;
    private readonly MessageService _messageService;
    private readonly ChatService _chatService;
    private readonly X509Certificate2 _certificate;
    private readonly List<ClientHandler> _clients = [];
    private readonly object _clientsLock = new();
    private CancellationTokenSource? _cts;

    public int Port { get; }
    public bool IsRunning { get; private set; }

    public TcpServer(int port, X509Certificate2 certificate, AuthService authService, MessageService messageService, ChatService chatService)
    {
        Port = port;
        _certificate = certificate;
        _authService = authService;
        _messageService = messageService;
        _chatService = chatService;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();
        IsRunning = true;

        Console.WriteLine($"Server started on port {Port} (TLS enabled)");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = Task.Run(() => HandleClientAsync(tcpClient, _cts.Token), _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Server stopping
        }
        finally
        {
            Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        var stream = tcpClient.GetStream();
        var sslStream = new SslStream(stream, false);

        try
        {
            await sslStream.AuthenticateAsServerAsync(
                _certificate,
                clientCertificateRequired: false,
                SslProtocols.Tls12 | SslProtocols.Tls13,
                checkCertificateRevocation: false);

            Console.WriteLine("TLS handshake OK.");

            var handler = new ClientHandler(tcpClient, sslStream, _authService, _messageService, _chatService)
            {
                OnBroadcast = BroadcastToChatAsync
            };

            lock (_clientsLock)
            {
                _clients.Add(handler);
            }

            Console.WriteLine($"Client connected: {handler.CurrentUserId?.ToString() ?? "anonymous"}");
            await handler.HandleAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TLS handshake failed: {ex.Message}");
        }
        finally
        {
            lock (_clientsLock)
            {
                _clients.RemoveAll(c => !c.IsConnected);
            }
            sslStream.Dispose();
            tcpClient.Close();
            Console.WriteLine($"Client disconnected");
        }
    }

    public void Stop()
    {
        IsRunning = false;
        _listener.Stop();
        _cts?.Cancel();

        lock (_clientsLock)
        {
            foreach (var client in _clients)
            {
                client.Disconnect();
            }
            _clients.Clear();
        }
    }

    public async Task BroadcastToChatAsync(int chatId, Packet packet, int? excludeUserId, CancellationToken cancellationToken = default)
    {
        var memberIds = await _chatService.GetChatMemberIdsAsync(chatId, cancellationToken);

        List<ClientHandler> targets;
        lock (_clientsLock)
        {
            targets = _clients
                .Where(c => c.CurrentUserId.HasValue && memberIds.Contains(c.CurrentUserId.Value) && c.CurrentUserId != excludeUserId)
                .ToList();
        }

        foreach (var client in targets)
        {
            try
            {
                await client.SendPacketAsync(packet, cancellationToken);
            }
            catch
            {
                // Client may have disconnected
            }
        }
    }
}
