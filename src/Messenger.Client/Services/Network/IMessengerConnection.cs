using Messenger.Shared.Packets;

namespace Messenger.Client.Services.Network;

public interface IMessengerConnection
{
    bool IsConnected { get; }

    event EventHandler<Packet>? PacketReceived;

    event EventHandler? Disconnected;

    Task ConnectAsync(
        CancellationToken cancellationToken);

    Task DisconnectAsync(
        CancellationToken cancellationToken);

    Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : Packet
        where TResponse : Packet;
}
