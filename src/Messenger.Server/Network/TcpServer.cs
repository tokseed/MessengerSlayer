using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Messenger.Server.Database;
using Messenger.Server.Services;
using Messenger.Shared.Packets;

namespace Messenger.Server.Network;

public sealed class TcpServer
{
    private readonly TcpListener _listener;
    private readonly X509Certificate2 _certificate;
    private readonly DbContextOptions<MessengerDbContext> _dbContextOptions;

    private readonly List<ClientHandler> _clients =
        [];

    private readonly object _clientsLock =
        new();

    private CancellationTokenSource? _cts;

    public int Port
    {
        get;
    }

    public bool IsRunning
    {
        get;
        private set;
    }

    public TcpServer(
        int port,
        X509Certificate2 certificate,
        DbContextOptions<MessengerDbContext> dbContextOptions)
    {
        Port =
            port;

        _certificate =
            certificate ??
            throw new ArgumentNullException(
                nameof(certificate));

        _dbContextOptions =
            dbContextOptions ??
            throw new ArgumentNullException(
                nameof(dbContextOptions));

        _listener =
            new TcpListener(
                IPAddress.Any,
                port);
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        _cts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        _listener.Start();

        IsRunning =
            true;

        Console.WriteLine(
            $"Server started on port {Port} (TLS enabled)");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                TcpClient tcpClient =
                    await _listener.AcceptTcpClientAsync(
                        _cts.Token);

                _ =
                    Task.Run(
                        () =>
                            HandleClientAsync(
                                tcpClient,
                                _cts.Token),
                        CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            Stop();
        }
    }

    private async Task HandleClientAsync(
        TcpClient tcpClient,
        CancellationToken cancellationToken)
    {
        NetworkStream stream =
            tcpClient.GetStream();

        SslStream sslStream =
            new(
                stream,
                leaveInnerStreamOpen: false);

        ClientHandler? handler =
            null;

        bool tlsEstablished =
            false;

        try
        {
            await sslStream.AuthenticateAsServerAsync(
                _certificate,
                clientCertificateRequired: false,
                SslProtocols.Tls12 |
                SslProtocols.Tls13,
                checkCertificateRevocation: false);

            tlsEstablished =
                true;

            Console.WriteLine(
                "TLS handshake OK.");

            // DbContext is not thread-safe. Each connected client owns
            // an independent context and services for its session.
            await using MessengerDbContext db =
                new(
                    _dbContextOptions);

            AuthService authService =
                new(
                    db);

            MessageService messageService =
                new(
                    db);

            ChatService chatService =
                new(
                    db);

            handler =
                new ClientHandler(
                    tcpClient,
                    sslStream,
                    authService,
                    messageService,
                    chatService)
                {
                    OnBroadcast =
                        BroadcastToChatAsync
                };

            lock (_clientsLock)
            {
                _clients.Add(
                    handler);
            }

            Console.WriteLine(
                "Client connected.");

            await handler.HandleAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            string stage =
                tlsEstablished
                    ? "Client connection failed"
                    : "TLS handshake failed";

            Console.WriteLine(
                $"{stage}: {exception}");
        }
        finally
        {
            if (handler != null)
            {
                lock (_clientsLock)
                {
                    _clients.Remove(
                        handler);
                }
            }

            try
            {
                await sslStream.DisposeAsync();
            }
            catch
            {
            }

            try
            {
                tcpClient.Close();
            }
            catch
            {
            }

            Console.WriteLine(
                $"Client disconnected: " +
                $"{handler?.CurrentUserId?.ToString() ?? "anonymous"}");
        }
    }

    public void Stop()
    {
        IsRunning =
            false;

        _listener.Stop();

        _cts?.Cancel();

        lock (_clientsLock)
        {
            foreach (ClientHandler client
                     in _clients)
            {
                client.Disconnect();
            }

            _clients.Clear();
        }
    }

    public async Task BroadcastToChatAsync(
        int chatId,
        Packet packet,
        int? excludeUserId,
        CancellationToken cancellationToken = default)
    {
        // Broadcast is independent of any client-owned DbContext.
        await using MessengerDbContext db =
            new(
                _dbContextOptions);

        ChatService chatService =
            new(
                db);

        List<int> memberIds =
            await chatService.GetChatMemberIdsAsync(
                chatId,
                cancellationToken);

        List<ClientHandler> targets;

        lock (_clientsLock)
        {
            targets =
                _clients
                    .Where(
                        client =>
                            client.CurrentUserId.HasValue &&
                            memberIds.Contains(
                                client.CurrentUserId.Value) &&
                            client.CurrentUserId !=
                            excludeUserId)
                    .ToList();
        }

        foreach (ClientHandler client
                 in targets)
        {
            try
            {
                await client.SendPacketAsync(
                    packet,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    $"Broadcast failed for UserId=" +
                    $"{client.CurrentUserId?.ToString() ?? "anonymous"}: " +
                    exception.Message);
            }
        }
    }
}
