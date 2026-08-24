using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services.Authentication;
namespace Messenger.Client.ViewModels;
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    [ObservableProperty] private bool _notificationsEnabled = true;
    [ObservableProperty] private bool _launchOnStartup;
    [ObservableProperty] private bool _isBusy;
    public SettingsViewModel(string username, IAuthenticationService authenticationService)
    {
        Username = username;
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
    }
    public event EventHandler? BackRequested;
    public event EventHandler? LogoutCompleted;
    public string Username { get; }
    [RelayCommand] private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await _authenticationService.LogoutAsync(CancellationToken.None); LogoutCompleted?.Invoke(this, EventArgs.Empty); }
        finally { IsBusy = false; }
    }
}
