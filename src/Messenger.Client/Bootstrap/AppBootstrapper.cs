using Messenger.Client.Configuration;
using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Chats;
using Messenger.Client.Services.Navigation;
using Messenger.Client.Services.Network;
using Messenger.Client.Services.Threading;
using Messenger.Client.ViewModels;
using Messenger.Client.Views;
namespace Messenger.Client.Bootstrap;
public sealed class AppBootstrapper
{
    public MainWindow CreateMainWindow()
    {
        ClientEndpointOptions endpointOptions = ClientEndpointOptions.CreateDevelopmentDefault();
        IMessengerConnection messengerConnection = new MessengerConnectionStub(endpointOptions);
        IAuthenticationService authenticationService = new LocalAuthenticationService(messengerConnection);
        IChatService chatService = new EmptyChatService();
        INavigationService navigationService = new NavigationService();
        IUiDispatcher uiDispatcher = new AvaloniaUiDispatcher();
        ShellViewModel shellViewModel = new ShellViewModel(navigationService, authenticationService, chatService, uiDispatcher);
        return new MainWindow { DataContext = shellViewModel };
    }
}
