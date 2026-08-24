using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services.Authentication;
namespace Messenger.Client.ViewModels;
public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(LoginCommand))] private string _username = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(LoginCommand))] private string _password = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(LoginCommand))] private bool _isBusy;
    public LoginViewModel(IAuthenticationService authenticationService) => _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
    public event EventHandler<string>? LoginSucceeded;
    public event EventHandler? RegisterRequested;
    private bool CanLogin() => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true; StatusText = string.Empty;
        try
        {
            bool success = await _authenticationService.LoginAsync(Username.Trim(), Password, CancellationToken.None);
            if (!success) { StatusText = "Проверьте введённые данные."; return; }
            LoginSucceeded?.Invoke(this, Username.Trim());
        }
        catch (OperationCanceledException) { StatusText = "Операция отменена."; }
        catch { StatusText = "Не удалось выполнить вход."; }
        finally { IsBusy = false; }
    }
    [RelayCommand] private void OpenRegister() => RegisterRequested?.Invoke(this, EventArgs.Empty);
}
