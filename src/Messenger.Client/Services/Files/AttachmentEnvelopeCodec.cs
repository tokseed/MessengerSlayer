using System.Text.Json;

namespace Messenger.Client.Services.Files;

public static class AttachmentEnvelopeCodec
{
    public const int MaximumFileBytes =
        384 * 1024;

    private const string Prefix =
        "[[MessengerSlayer.File.v1]]";

    public static string Encode(
        string fileName,
        byte[] data)
    {
        ArgumentNullException.ThrowIfNull(
            data);

        if (data.Length == 0)
        {
            throw new ArgumentException(
                "The file is empty.",
                nameof(data));
        }

        if (data.Length > MaximumFileBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(data),
                $"Maximum compatibility attachment size is {MaximumFileBytes} bytes.");
        }

        string safeName =
            Path.GetFileName(
                fileName);

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName =
                "file";
        }

        AttachmentEnvelope envelope =
            new()
            {
                FileName =
                    safeName,
                DataBase64 =
                    Convert.ToBase64String(
                        data)
            };

        return Prefix +
               JsonSerializer.Serialize(
                   envelope);
    }

    public static bool TryDecode(
        string content,
        out AttachmentEnvelope? envelope,
        out byte[] data)
    {
        envelope =
            null;

        data =
            Array.Empty<byte>();

        if (string.IsNullOrEmpty(content) ||
            !content.StartsWith(
                Prefix,
                StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            string json =
                content[Prefix.Length..];

            AttachmentEnvelope? parsed =
                JsonSerializer.Deserialize<AttachmentEnvelope>(
                    json);

            if (parsed == null ||
                string.IsNullOrWhiteSpace(parsed.FileName) ||
                string.IsNullOrWhiteSpace(parsed.DataBase64))
            {
                return false;
            }

            byte[] decoded =
                Convert.FromBase64String(
                    parsed.DataBase64);

            if (decoded.Length == 0 ||
                decoded.Length > MaximumFileBytes)
            {
                return false;
            }

            envelope =
                parsed;

            data =
                decoded;

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
