using Messenger.Client.Configuration;
using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Chats;
using Messenger.Client.Services.Files;
using Messenger.Client.Services.Navigation;
using Messenger.Client.Services.State;
using Messenger.Client.Services.Threading;

namespace Messenger.Client.ViewModels;

public sealed partial class ShellViewModel :
    ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ClientSessionState _sessionState;
    private readonly IChatService _chatService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IFilePickerService _filePickerService;
    private readonly IClientChatStateStore _stateStore;
    private readonly ClientEndpointOptions _endpointOptions;

    private string _currentUsername =
        string.Empty;

    public ShellViewModel(
        INavigationService navigationService,
        IAuthenticationService authenticationService,
        ClientSessionState sessionState,
        IChatService chatService,
        IUiDispatcher uiDispatcher,
        IFilePickerService filePickerService,
        IClientChatStateStore stateStore,
        ClientEndpointOptions endpointOptions)
    {
        _navigationService =
            navigationService;

        _authenticationService =
            authenticationService;

        _sessionState =
            sessionState;

        _chatService =
            chatService;

        _uiDispatcher =
            uiDispatcher;

        _filePickerService =
            filePickerService;

        _stateStore =
            stateStore;

        _endpointOptions =
            endpointOptions;

        _navigationService.CurrentViewModelChanged +=
            OnCurrentViewModelChanged;

        NavigateToLogin();
    }

    public ViewModelBase? CurrentPage =>
        _navigationService.CurrentViewModel;

    private void NavigateToLogin()
    {
        LoginViewModel viewModel =
            new(
                _authenticationService);

        viewModel.LoginSucceeded +=
            (_, username) =>
            {
                _currentUsername =
                    username;

                NavigateToChats();
            };

        viewModel.RegisterRequested +=
            (_, _) =>
                NavigateToRegister();

        Navigate(
            viewModel);
    }

    private void NavigateToRegister()
    {
        RegisterViewModel viewModel =
            new(
                _authenticationService,
                _filePickerService);

        viewModel.BackRequested +=
            (_, _) =>
                NavigateToLogin();

        viewModel.RegisterSucceeded +=
            (_, username) =>
            {
                _currentUsername =
                    username;

                NavigateToChats();
            };

        Navigate(
            viewModel);
    }

    private void NavigateToChats()
    {
        _stateStore.SetCurrentUser(
            _currentUsername);

        ChatsViewModel viewModel =
            new(
                _currentUsername,
                _sessionState.AvatarUrl,
                _chatService,
                _uiDispatcher,
                _filePickerService,
                _stateStore,
                _endpointOptions);

        viewModel.CreateChatRequested +=
            (_, _) =>
                NavigateToCreateChat();

        viewModel.SettingsRequested +=
            (_, _) =>
                NavigateToSettings();

        Navigate(
            viewModel);
    }

    private void NavigateToCreateChat()
    {
        CreateChatViewModel viewModel =
            new(
                _chatService);

        viewModel.BackRequested +=
            (_, _) =>
                NavigateToChats();

        viewModel.ChatCreated +=
            (_, _) =>
                NavigateToChats();

        Navigate(
            viewModel);
    }

    private void NavigateToSettings()
    {
        SettingsViewModel viewModel =
            new(
                _currentUsername,
                _authenticationService,
                _filePickerService);

        viewModel.BackRequested +=
            (_, _) =>
                NavigateToChats();

        viewModel.LogoutCompleted +=
            (_, _) =>
            {
                _currentUsername =
                    string.Empty;

                NavigateToLogin();
            };

        Navigate(
            viewModel);
    }

    private void Navigate(
        ViewModelBase viewModel)
    {
        if (_navigationService.CurrentViewModel
            is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _navigationService.NavigateTo(
            viewModel);
    }

    private void OnCurrentViewModelChanged(
        object? sender,
        EventArgs eventArgs)
    {
        OnPropertyChanged(
            nameof(CurrentPage));
    }
}
