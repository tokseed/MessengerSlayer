namespace Messenger.Client.Services.Files;

public interface IFilePickerService
{
    Task<PickedFile?> PickFileAsync(
        CancellationToken cancellationToken);

    Task<PickedFile?> PickAvatarAsync(
        CancellationToken cancellationToken);

    Task<bool> SaveFileAsync(
        string suggestedFileName,
        byte[] data,
        CancellationToken cancellationToken);
}
