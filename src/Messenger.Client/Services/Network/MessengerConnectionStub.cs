using System;
using System.Threading;
using System.Threading.Tasks;
using Messenger.Client.Configuration;

namespace Messenger.Client.Services.Network;

public sealed class MessengerConnectionStub :
    IMessengerConnection
{
    private readonly ClientEndpointOptions
        _endpointOptions;

    public MessengerConnectionStub(
        ClientEndpointOptions endpointOptions)
    {
        _endpointOptions =
            endpointOptions ??
            throw new ArgumentNullException(
                nameof(endpointOptions));
    }

    public bool IsConnected { get; private set; }

    public Task ConnectAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        _ =
            _endpointOptions.Host;

        IsConnected = true;

        return Task.CompletedTask;
    }

    public Task DisconnectAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        IsConnected = false;

        return Task.CompletedTask;
    }

    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        cancellationToken
            .ThrowIfCancellationRequested();

        throw new InvalidOperationException(
            "The real Messenger.Shared/TCP transport has not been connected yet.");
    }
}
