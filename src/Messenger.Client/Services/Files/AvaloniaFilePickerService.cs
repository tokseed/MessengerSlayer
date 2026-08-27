using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Messenger.Client.Services.Files;

public sealed class AvaloniaFilePickerService :
    IFilePickerService
{
    public Task<PickedFile?> PickFileAsync(
        CancellationToken cancellationToken)
    {
        return PickAsync(
            new FilePickerOpenOptions
            {
                Title =
                    "Выберите файл",
                AllowMultiple =
                    false
            },
            cancellationToken);
    }

    public Task<PickedFile?> PickAvatarAsync(
        CancellationToken cancellationToken)
    {
        return PickAsync(
            new FilePickerOpenOptions
            {
                Title =
                    "Выберите аватар",
                AllowMultiple =
                    false,
                FileTypeFilter =
                    new[]
                    {
                        new FilePickerFileType(
                            "Изображения")
                        {
                            Patterns =
                                new[]
                                {
                                    "*.png",
                                    "*.jpg",
                                    "*.jpeg",
                                    "*.webp"
                                }
                        }
                    }
            },
            cancellationToken);
    }

    public async Task<bool> SaveFileAsync(
        string suggestedFileName,
        byte[] data,
        CancellationToken cancellationToken)
    {
        IStorageProvider? storageProvider =
            GetStorageProvider();

        if (storageProvider == null ||
            !storageProvider.CanSave)
        {
            return false;
        }

        IStorageFile? file =
            await storageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title =
                        "Сохранить файл",
                    SuggestedFileName =
                        suggestedFileName
                });

        if (file == null)
        {
            return false;
        }

        await using Stream destination =
            await file.OpenWriteAsync();

        await destination.WriteAsync(
            data,
            cancellationToken);

        await destination.FlushAsync(
            cancellationToken);

        return true;
    }

    private static async Task<PickedFile?> PickAsync(
        FilePickerOpenOptions options,
        CancellationToken cancellationToken)
    {
        IStorageProvider? storageProvider =
            GetStorageProvider();

        if (storageProvider == null ||
            !storageProvider.CanOpen)
        {
            return null;
        }

        IReadOnlyList<IStorageFile> files =
            await storageProvider.OpenFilePickerAsync(
                options);

        IStorageFile? file =
            files.FirstOrDefault();

        if (file == null)
        {
            return null;
        }

        await using Stream source =
            await file.OpenReadAsync();

        using MemoryStream memory =
            new();

        await source.CopyToAsync(
            memory,
            cancellationToken);

        return new PickedFile
        {
            Name =
                file.Name,
            Data =
                memory.ToArray()
        };
    }

    private static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        return desktop.MainWindow?
            .StorageProvider;
    }
}
