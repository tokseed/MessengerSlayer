using System.Threading;
using System.Threading.Tasks;

namespace Messenger.Client.Services.Network;

public interface IMessengerConnection
{
    bool IsConnected { get; }

    Task ConnectAsync(
        CancellationToken cancellationToken);

    Task DisconnectAsync(
        CancellationToken cancellationToken);

    Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class;
}
