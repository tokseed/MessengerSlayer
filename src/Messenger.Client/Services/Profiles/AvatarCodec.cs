using Avalonia.Media.Imaging;
using Messenger.Client.Services.Files;

namespace Messenger.Client.Services.Profiles;

public static class AvatarCodec
{
    public const int MaximumAvatarBytes =
        256 * 1024;

    public static bool TryEncode(
        PickedFile file,
        out string dataUri,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(
            file);

        dataUri =
            string.Empty;

        error =
            string.Empty;

        if (file.Data.Length == 0)
        {
            error =
                "Файл изображения пуст.";

            return false;
        }

        if (file.Data.Length >
            MaximumAvatarBytes)
        {
            error =
                "Аватар должен быть не больше 256 КБ.";

            return false;
        }

        string? mimeType =
            Path.GetExtension(
                    file.Name)
                .ToLowerInvariant() switch
            {
                ".png" =>
                    "image/png",
                ".jpg" or ".jpeg" =>
                    "image/jpeg",
                ".webp" =>
                    "image/webp",
                _ =>
                    null
            };

        if (mimeType == null)
        {
            error =
                "Поддерживаются PNG, JPG, JPEG и WEBP.";

            return false;
        }

        try
        {
            using MemoryStream validation =
                new(
                    file.Data,
                    writable: false);

            using Bitmap bitmap =
                new(
                    validation);

            dataUri =
                $"data:{mimeType};base64,{Convert.ToBase64String(file.Data)}";

            return true;
        }
        catch
        {
            error =
                "Не удалось прочитать изображение.";

            return false;
        }
    }

    public static Bitmap? TryCreateBitmap(
        string? dataUri)
    {
        if (string.IsNullOrWhiteSpace(
                dataUri))
        {
            return null;
        }

        int commaIndex =
            dataUri.IndexOf(',');

        if (commaIndex < 0 ||
            commaIndex ==
            dataUri.Length - 1)
        {
            return null;
        }

        try
        {
            byte[] bytes =
                Convert.FromBase64String(
                    dataUri[(commaIndex + 1)..]);

            using MemoryStream stream =
                new(
                    bytes,
                    writable: false);

            return new Bitmap(
                stream);
        }
        catch
        {
            return null;
        }
    }
}
