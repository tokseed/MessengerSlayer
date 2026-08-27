using System.Net.Mail;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Files;
using Messenger.Client.Services.Profiles;
using Messenger.Shared.Models;

namespace Messenger.Client.ViewModels;

public sealed partial class SettingsViewModel :
    ViewModelBase,
    IDisposable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IFilePickerService _filePickerService;

    private string? _avatarDataUri;

    [ObservableProperty]
    private string _firstName =
        string.Empty;

    [ObservableProperty]
    private string _lastName =
        string.Empty;

    [ObservableProperty]
    private string _phoneNumber =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]
    private string _email =
        string.Empty;

    [ObservableProperty]
    private Bitmap? _avatarImage;

    [ObservableProperty]
    private bool _hasAvatar;

    [ObservableProperty]
    private string _profileStatusText =
        string.Empty;

    [ObservableProperty]
    private bool _notificationsEnabled =
        true;

    [ObservableProperty]
    private bool _launchOnStartup;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]
    private bool _isBusy;

    public SettingsViewModel(
        string username,
        IAuthenticationService authenticationService,
        IFilePickerService filePickerService)
    {
        Username =
            username;

        _authenticationService =
            authenticationService ??
            throw new ArgumentNullException(
                nameof(authenticationService));

        _filePickerService =
            filePickerService ??
            throw new ArgumentNullException(
                nameof(filePickerService));

        _ =
            LoadProfileAsync();
    }

    public event EventHandler?
        BackRequested;

    public event EventHandler?
        LogoutCompleted;

    public string Username { get; }

    public bool HasNoAvatar =>
        !HasAvatar;

    public string DisplayName
    {
        get
        {
            string fullName =
                $"{FirstName} {LastName}".Trim();

            return string.IsNullOrWhiteSpace(
                    fullName)
                ? Username
                : fullName;
        }
    }

    partial void OnFirstNameChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(DisplayName));
    }

    partial void OnLastNameChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(DisplayName));
    }

    partial void OnHasAvatarChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(HasNoAvatar));
    }

    private bool CanSaveProfile()
    {
        return !IsBusy &&
               MailAddress.TryCreate(
                   Email.Trim(),
                   out _);
    }

    [RelayCommand]
    private void Back()
    {
        BackRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    [RelayCommand]
    private async Task ChooseAvatarAsync()
    {
        PickedFile? file =
            await _filePickerService.PickAvatarAsync(
                CancellationToken.None);

        if (file == null)
        {
            return;
        }

        if (!AvatarCodec.TryEncode(
                file,
                out string dataUri,
                out string error))
        {
            ProfileStatusText =
                error;

            return;
        }

        SetAvatar(
            dataUri);

        ProfileStatusText =
            "Нажмите «Сохранить профиль», чтобы применить аватар.";
    }

    [RelayCommand]
    private void RemoveAvatar()
    {
        SetAvatar(
            null);

        ProfileStatusText =
            "Нажмите «Сохранить профиль», чтобы удалить аватар.";
    }

    [RelayCommand(CanExecute = nameof(CanSaveProfile))]
    private async Task SaveProfileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!MailAddress.TryCreate(
                Email.Trim(),
                out _))
        {
            ProfileStatusText =
                "Введите корректный email.";

            return;
        }

        IsBusy =
            true;

        ProfileStatusText =
            "Сохранение...";

        try
        {
            UserDto? updated =
                await _authenticationService.UpdateProfileAsync(
                    Email,
                    _avatarDataUri,
                    CancellationToken.None);

            if (updated == null)
            {
                ProfileStatusText =
                    "Не удалось сохранить профиль. Возможно, email уже используется.";

                return;
            }

            ApplyProfile(
                updated);

            ProfileStatusText =
                "Профиль сохранён.";
        }
        catch
        {
            ProfileStatusText =
                "Не удалось сохранить профиль.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy =
            true;

        try
        {
            await _authenticationService.LogoutAsync(
                CancellationToken.None);

            LogoutCompleted?.Invoke(
                this,
                EventArgs.Empty);
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private async Task LoadProfileAsync()
    {
        IsBusy =
            true;

        try
        {
            UserDto? profile =
                await _authenticationService.GetProfileAsync(
                    CancellationToken.None);

            if (profile == null)
            {
                ProfileStatusText =
                    "Не удалось загрузить профиль.";

                return;
            }

            ApplyProfile(
                profile);
        }
        catch
        {
            ProfileStatusText =
                "Не удалось загрузить профиль.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private void ApplyProfile(
        UserDto profile)
    {
        FirstName =
            profile.FirstName;

        LastName =
            profile.LastName;

        PhoneNumber =
            profile.PhoneNumber;

        Email =
            profile.Email ??
            string.Empty;

        SetAvatar(
            profile.AvatarUrl);
    }

    private void SetAvatar(
        string? avatarDataUri)
    {
        AvatarImage?.Dispose();

        _avatarDataUri =
            avatarDataUri;

        AvatarImage =
            AvatarCodec.TryCreateBitmap(
                avatarDataUri);

        HasAvatar =
            AvatarImage != null;
    }

    public void Dispose()
    {
        AvatarImage?.Dispose();

        AvatarImage =
            null;
    }
}
