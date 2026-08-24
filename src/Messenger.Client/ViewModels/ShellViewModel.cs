using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Chats;
using Messenger.Client.Services.Navigation;
using Messenger.Client.Services.Threading;
namespace Messenger.Client.ViewModels;
public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IChatService _chatService;
    private readonly IUiDispatcher _uiDispatcher;
    private string _currentUsername = string.Empty;
    public ShellViewModel(INavigationService navigationService, IAuthenticationService authenticationService, IChatService chatService, IUiDispatcher uiDispatcher)
    {
        _navigationService = navigationService;
        _authenticationService = authenticationService;
        _chatService = chatService;
        _uiDispatcher = uiDispatcher;
        _navigationService.CurrentViewModelChanged += OnCurrentViewModelChanged;
        NavigateToLogin();
    }
    public ViewModelBase? CurrentPage => _navigationService.CurrentViewModel;
    private void NavigateToLogin()
    {
        var vm = new LoginViewModel(_authenticationService);
        vm.LoginSucceeded += (_, username) => { _currentUsername = username; NavigateToChats(); };
        vm.RegisterRequested += (_, _) => NavigateToRegister();
        _navigationService.NavigateTo(vm);
    }
    private void NavigateToRegister()
    {
        var vm = new RegisterViewModel(_authenticationService);
        vm.BackRequested += (_, _) => NavigateToLogin();
        vm.RegisterSucceeded += (_, username) => { _currentUsername = username; NavigateToChats(); };
        _navigationService.NavigateTo(vm);
    }
    private void NavigateToChats()
    {
        var vm = new ChatsViewModel(_currentUsername, _chatService, _uiDispatcher);
        vm.CreateChatRequested += (_, _) => NavigateToCreateChat();
        vm.SettingsRequested += (_, _) => NavigateToSettings();
        _navigationService.NavigateTo(vm);
    }
    private void NavigateToCreateChat()
    {
        var vm = new CreateChatViewModel(_chatService);
        vm.BackRequested += (_, _) => NavigateToChats();
        vm.ChatCreated += (_, _) => NavigateToChats();
        _navigationService.NavigateTo(vm);
    }
    private void NavigateToSettings()
    {
        var vm = new SettingsViewModel(_currentUsername, _authenticationService);
        vm.BackRequested += (_, _) => NavigateToChats();
        vm.LogoutCompleted += (_, _) => { _currentUsername = string.Empty; NavigateToLogin(); };
        _navigationService.NavigateTo(vm);
    }
    private void OnCurrentViewModelChanged(object? sender, System.EventArgs eventArgs) => OnPropertyChanged(nameof(CurrentPage));
}
