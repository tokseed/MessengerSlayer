using Messenger.Client.Configuration;
using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Chats;
using Messenger.Client.Services.Files;
using Messenger.Client.Services.Navigation;
using Messenger.Client.Services.Network;
using Messenger.Client.Services.State;
using Messenger.Client.Services.Threading;
using Messenger.Client.ViewModels;
using Messenger.Client.Views;

namespace Messenger.Client.Bootstrap;

public sealed class AppBootstrapper
{
    public MainWindow CreateMainWindow()
    {
        ClientEndpointOptions endpointOptions =
            ClientEndpointOptions.Load();

        IMessengerConnection messengerConnection =
            new TcpMessengerConnection(
                endpointOptions);

        ClientSessionState sessionState =
            new();

        IAuthenticationService authenticationService =
            new AuthenticationService(
                messengerConnection,
                sessionState);

        IChatService chatService =
            new ChatService(
                messengerConnection,
                sessionState);

        INavigationService navigationService =
            new NavigationService();

        IUiDispatcher uiDispatcher =
            new AvaloniaUiDispatcher();

        IFilePickerService filePickerService =
            new AvaloniaFilePickerService();

        IClientChatStateStore stateStore =
            new JsonClientChatStateStore();

        ShellViewModel shellViewModel =
            new(
                navigationService,
                authenticationService,
                sessionState,
                chatService,
                uiDispatcher,
                filePickerService,
                stateStore,
                endpointOptions);

        return new MainWindow
        {
            DataContext =
                shellViewModel
        };
    }
}
