using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Messenger.Client.Configuration;
using Messenger.Client.Diagnostics;
using Messenger.Shared.Network;
using Messenger.Shared.Packets;

namespace Messenger.Client.Services.Network;

public sealed class TcpMessengerConnection :
    IMessengerConnection,
    IAsyncDisposable
{
    private const int MaximumPacketBytes =
        1024 * 1024;

    // SHA-256 fingerprint of the unchanged team certificate:
    // src/Messenger.Server/Certs/server.crt
    private const string PinnedServerCertificateSha256 =
        "E5611F78A92904332C207895C9C6EC360424892919422FEC810C5D6F09493602";

    private readonly ClientEndpointOptions _options;
    private readonly SemaphoreSlim _requestLock =
        new(1, 1);
    private readonly SemaphoreSlim _sendLock =
        new(1, 1);
    private readonly object _pendingGate =
        new();

    private TcpClient? _client;
    private Stream? _stream;
    private CancellationTokenSource? _listenCancellation;
    private Task? _listenTask;
    private TaskCompletionSource<Packet>? _pendingResponse;
    private Type? _pendingResponseType;

    public TcpMessengerConnection(
        ClientEndpointOptions options)
    {
        _options =
            options ??
            throw new ArgumentNullException(
                nameof(options));
    }

    public bool IsConnected =>
        _client?.Connected == true &&
        _stream != null;

    public event EventHandler<Packet>?
        PacketReceived;

    public event EventHandler?
        Disconnected;

    public async Task ConnectAsync(
        CancellationToken cancellationToken)
    {
        if (IsConnected)
        {
            return;
        }

        await CleanupTransportAsync();

        TcpClient client =
            new();

        try
        {
            NetworkDiagnosticLog.Write(
                $"Connecting TCP to {_options.Host}:{_options.Port}...");

            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                cancellationToken);

            NetworkDiagnosticLog.Write(
                "TCP connection established.");

            Stream stream =
                client.GetStream();

            if (_options.UseTls)
            {
                SslStream sslStream =
                    new(
                        stream,
                        leaveInnerStreamOpen: false,
                        ValidateServerCertificate);

                try
                {
                    NetworkDiagnosticLog.Write(
                        $"Starting TLS. TargetHost={_options.Host}.");

                    await sslStream.AuthenticateAsClientAsync(
                        new SslClientAuthenticationOptions
                        {
                            TargetHost =
                                _options.Host,
                            EnabledSslProtocols =
                                SslProtocols.Tls12 |
                                SslProtocols.Tls13,
                            CertificateRevocationCheckMode =
                                X509RevocationMode.NoCheck
                        },
                        cancellationToken);

                    NetworkDiagnosticLog.Write(
                        $"TLS handshake OK. Protocol={sslStream.SslProtocol}, Cipher={sslStream.NegotiatedCipherSuite}.");
                }
                catch (Exception exception)
                {
                    NetworkDiagnosticLog.WriteException(
                        "TLS handshake failed",
                        exception);

                    await sslStream.DisposeAsync();
                    throw;
                }

                stream =
                    sslStream;
            }

            _client =
                client;

            _stream =
                stream;

            _listenCancellation =
                new CancellationTokenSource();

            _listenTask =
                ListenAsync(
                    _listenCancellation.Token);
        }
        catch
        {
            try
            {
                client.Dispose();
            }
            catch
            {
            }

            throw;
        }
    }

    public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : Packet
        where TResponse : Packet
    {
        ArgumentNullException.ThrowIfNull(
            request);

        await _requestLock.WaitAsync(
            cancellationToken);

        TaskCompletionSource<Packet> completion =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            EnsureConnected();

            lock (_pendingGate)
            {
                _pendingResponse =
                    completion;

                _pendingResponseType =
                    typeof(TResponse);
            }

            await SendPacketAsync(
                request,
                cancellationToken);

            using CancellationTokenSource timeout =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeout.CancelAfter(
                TimeSpan.FromSeconds(10));

            Packet response =
                await completion.Task.WaitAsync(
                    timeout.Token);

            if (response is not TResponse typed)
            {
                throw new InvalidOperationException(
                    $"Expected {typeof(TResponse).Name}, received {response.GetType().Name}.");
            }

            return typed;
        }
        catch (OperationCanceledException)
        {
            await CleanupTransportAsync();
            throw;
        }
        finally
        {
            lock (_pendingGate)
            {
                if (ReferenceEquals(
                        _pendingResponse,
                        completion))
                {
                    _pendingResponse =
                        null;

                    _pendingResponseType =
                        null;
                }
            }

            _requestLock.Release();
        }
    }

    public async Task DisconnectAsync(
        CancellationToken cancellationToken)
    {
        if (IsConnected)
        {
            try
            {
                await SendPacketAsync(
                    new DisconnectPacket(),
                    cancellationToken);
            }
            catch (IOException)
            {
            }
            catch (SocketException)
            {
            }
        }

        await CleanupTransportAsync();
    }

    private async Task ListenAsync(
        CancellationToken cancellationToken)
    {
        Exception? failure =
            null;

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _stream != null)
            {
                Packet? packet =
                    await ReceivePacketExactlyAsync(
                        _stream,
                        cancellationToken);

                if (packet == null)
                {
                    break;
                }

                if (TryCompletePendingResponse(
                        packet))
                {
                    continue;
                }

                PacketReceived?.Invoke(
                    this,
                    packet);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            failure =
                exception;

            NetworkDiagnosticLog.WriteException(
                "Receive loop failed",
                exception);
        }
        finally
        {
            TaskCompletionSource<Packet>? pending;

            lock (_pendingGate)
            {
                pending =
                    _pendingResponse;

                _pendingResponse =
                    null;

                _pendingResponseType =
                    null;
            }

            if (failure != null)
            {
                pending?.TrySetException(
                    failure);
            }
            else
            {
                pending?.TrySetException(
                    new IOException(
                        "The connection was closed."));
            }

            Disconnected?.Invoke(
                this,
                EventArgs.Empty);
        }
    }

    private bool TryCompletePendingResponse(
        Packet packet)
    {
        TaskCompletionSource<Packet>? completion =
            null;

        lock (_pendingGate)
        {
            if (_pendingResponse != null &&
                _pendingResponseType != null &&
                _pendingResponseType.IsInstanceOfType(
                    packet))
            {
                completion =
                    _pendingResponse;

                _pendingResponse =
                    null;

                _pendingResponseType =
                    null;
            }
        }

        if (completion == null)
        {
            return false;
        }

        completion.TrySetResult(
            packet);

        return true;
    }

    private async Task SendPacketAsync(
        Packet packet,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        Stream stream =
            _stream!;

        byte[] frame =
            PacketSerializer.Serialize(
                packet);

        await _sendLock.WaitAsync(
            cancellationToken);

        try
        {
            await stream.WriteAsync(
                frame,
                cancellationToken);

            await stream.FlushAsync(
                cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static async Task<Packet?> ReceivePacketExactlyAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        byte[] lengthBuffer =
            new byte[4];

        if (!await ReadExactlyAsync(
                stream,
                lengthBuffer,
                cancellationToken))
        {
            return null;
        }

        int length =
            BitConverter.ToInt32(
                lengthBuffer);

        if (length <= 0 ||
            length > MaximumPacketBytes)
        {
            throw new IOException(
                "Incoming packet length is invalid.");
        }

        byte[] payload =
            new byte[length];

        if (!await ReadExactlyAsync(
                stream,
                payload,
                cancellationToken))
        {
            return null;
        }

        return PacketSerializer.Deserialize(
            payload);
    }

    private static async Task<bool> ReadExactlyAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        int offset =
            0;

        while (offset < buffer.Length)
        {
            int read =
                await stream.ReadAsync(
                    buffer[offset..],
                    cancellationToken);

            if (read == 0)
            {
                return false;
            }

            offset +=
                read;
        }

        return true;
    }

    private bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (certificate == null)
        {
            NetworkDiagnosticLog.Write(
                "TLS certificate validation failed: server provided no certificate.");

            return false;
        }

        try
        {
            byte[] rawCertificate =
                certificate.GetRawCertData();

            string presentedFingerprint =
                Convert.ToHexString(
                    SHA256.HashData(
                        rawCertificate));

            bool matches =
                string.Equals(
                    presentedFingerprint,
                    PinnedServerCertificateSha256,
                    StringComparison.OrdinalIgnoreCase);

            NetworkDiagnosticLog.Write(
                $"TLS certificate: Subject={certificate.Subject}, " +
                $"PolicyErrors={sslPolicyErrors}, " +
                $"SHA256={presentedFingerprint}, " +
                $"Pinned={PinnedServerCertificateSha256}, " +
                $"Match={matches}.");

            return matches;
        }
        catch (Exception exception)
        {
            NetworkDiagnosticLog.WriteException(
                "TLS certificate validation threw",
                exception);

            return false;
        }
    }

    private void EnsureConnected()
    {
        if (!IsConnected ||
            _stream == null)
        {
            throw new InvalidOperationException(
                "The client is not connected to Messenger.Server.");
        }
    }

    private async Task CleanupTransportAsync()
    {
        CancellationTokenSource? listenCancellation =
            _listenCancellation;

        Task? listenTask =
            _listenTask;

        _listenCancellation =
            null;

        _listenTask =
            null;

        if (listenCancellation != null)
        {
            listenCancellation.Cancel();
        }

        if (_stream != null)
        {
            try
            {
                await _stream.DisposeAsync();
            }
            catch
            {
            }

            _stream =
                null;
        }

        if (_client != null)
        {
            try
            {
                _client.Dispose();
            }
            catch
            {
            }

            _client =
                null;
        }

        if (listenTask != null)
        {
            try
            {
                await listenTask;
            }
            catch
            {
            }
        }

        listenCancellation?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupTransportAsync();

        _requestLock.Dispose();
        _sendLock.Dispose();
    }
}
