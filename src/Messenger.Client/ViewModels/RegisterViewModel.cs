using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services.Authentication;
namespace Messenger.Client.ViewModels;
public sealed partial class RegisterViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(RegisterCommand))] private string _username = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(RegisterCommand))] private string _displayName = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(RegisterCommand))] private string _password = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(RegisterCommand))] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(RegisterCommand))] private bool _isBusy;
    public RegisterViewModel(IAuthenticationService authenticationService) => _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
    public event EventHandler? BackRequested;
    public event EventHandler<string>? RegisterSucceeded;
    private bool CanRegister() => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(DisplayName) && !string.IsNullOrWhiteSpace(Password) && Password == ConfirmPassword;
    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        IsBusy = true; StatusText = string.Empty;
        try
        {
            bool success = await _authenticationService.RegisterAsync(Username.Trim(), DisplayName.Trim(), Password, CancellationToken.None);
            if (!success) { StatusText = "Не удалось создать учётную запись."; return; }
            RegisterSucceeded?.Invoke(this, Username.Trim());
        }
        catch { StatusText = "Не удалось создать учётную запись."; }
        finally { IsBusy = false; }
    }
    [RelayCommand] private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
}
