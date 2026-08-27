using System.IO;
using System.Net.Mail;
using System.Net.Sockets;
using System.Security.Authentication;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Files;
using Messenger.Client.Services.Profiles;

namespace Messenger.Client.ViewModels;

public sealed partial class RegisterViewModel :
    ViewModelBase,
    IDisposable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IFilePickerService _filePickerService;
    private readonly IClientProfileService _profileService;

    private string? _avatarDataUri;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _username =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _firstName =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _lastName =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _phoneNumber =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _email =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _password =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string _confirmPassword =
        string.Empty;

    [ObservableProperty]
    private Bitmap? _avatarImage;

    [ObservableProperty]
    private bool _hasAvatar;

    [ObservableProperty]
    private string _statusText =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private bool _isBusy;

    public RegisterViewModel(
        IAuthenticationService authenticationService,
        IFilePickerService filePickerService,
        IClientProfileService profileService)
    {
        _authenticationService =
            authenticationService ??
            throw new ArgumentNullException(
                nameof(authenticationService));

        _filePickerService =
            filePickerService ??
            throw new ArgumentNullException(
                nameof(filePickerService));

        _profileService =
            profileService ??
            throw new ArgumentNullException(
                nameof(profileService));
    }

    public event EventHandler?
        BackRequested;

    public event EventHandler<string>?
        RegisterSucceeded;

    public bool HasNoAvatar =>
        !HasAvatar;

    partial void OnHasAvatarChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(HasNoAvatar));
    }

    private bool CanRegister()
    {
        return !IsBusy &&
               !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName) &&
               !string.IsNullOrWhiteSpace(PhoneNumber) &&
               MailAddress.TryCreate(
                   Email.Trim(),
                   out _) &&
               !string.IsNullOrWhiteSpace(Password) &&
               Password ==
               ConfirmPassword;
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
            StatusText =
                error;

            return;
        }

        SetAvatar(
            dataUri);

        StatusText =
            string.Empty;
    }

    [RelayCommand]
    private void RemoveAvatar()
    {
        SetAvatar(
            null);

        StatusText =
            string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        IsBusy =
            true;

        StatusText =
            string.Empty;

        try
        {
            bool success =
                await _authenticationService.RegisterAsync(
                    Username,
                    FirstName,
                    LastName,
                    PhoneNumber,
                    Password,
                    CancellationToken.None);

            if (!success)
            {
                StatusText =
                    "Не удалось создать учётную запись.";

                return;
            }

            // The colleague RegisterPacket does not contain Email/AvatarUrl.
            // Preserve the UI without modifying Shared/Server: store them locally.
            await _profileService.SaveLocalOverridesAsync(
                Email,
                _avatarDataUri,
                CancellationToken.None);

            RegisterSucceeded?.Invoke(
                this,
                Username.Trim());
        }
        catch (OperationCanceledException)
        {
            StatusText =
                "Операция отменена.";
        }
        catch (AuthenticationException)
        {
            StatusText =
                "Не удалось установить защищённое соединение с сервером.";
        }
        catch (SocketException)
        {
            StatusText =
                "Сервер недоступен.";
        }
        catch (IOException)
        {
            StatusText =
                "Соединение с сервером было прервано.";
        }
        catch
        {
            StatusText =
                "Не удалось создать учётную запись.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        BackRequested?.Invoke(
            this,
            EventArgs.Empty);
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
