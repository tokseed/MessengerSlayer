using System.Text;
using System.Text.Json;
using Messenger.Shared.Packets;

namespace Messenger.Shared.Network;

public static class PacketSerializer
{
    private const int MaximumPacketBytes =
        1024 * 1024;

    private static readonly JsonSerializerOptions Options =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,
            WriteIndented =
                false
        };

    public static byte[] Serialize(
        Packet packet)
    {
        ArgumentNullException.ThrowIfNull(
            packet);

        string typeName =
            packet.GetType().Name;

        string json =
            JsonSerializer.Serialize(
                packet,
                packet.GetType(),
                Options);

        string payload =
            $"{typeName}\n{json}";

        byte[] bytes =
            Encoding.UTF8.GetBytes(
                payload);

        byte[] length =
            BitConverter.GetBytes(
                bytes.Length);

        return length
            .Concat(bytes)
            .ToArray();
    }

    public static Packet? Deserialize(
        byte[] data)
    {
        ArgumentNullException.ThrowIfNull(
            data);

        string payload =
            Encoding.UTF8.GetString(
                data);

        int newlineIndex =
            payload.IndexOf('\n');

        if (newlineIndex < 0)
        {
            return null;
        }

        string typeName =
            payload[..newlineIndex];

        string json =
            payload[(newlineIndex + 1)..];

        Type? type =
            Type.GetType(
                $"Messenger.Shared.Packets.{typeName}");

        if (type == null)
        {
            return null;
        }

        return JsonSerializer.Deserialize(
            json,
            type,
            Options) as Packet;
    }

    public static async Task SendAsync(
        Stream stream,
        Packet packet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            stream);

        ArgumentNullException.ThrowIfNull(
            packet);

        byte[] data =
            Serialize(
                packet);

        await stream.WriteAsync(
            data,
            cancellationToken);

        await stream.FlushAsync(
            cancellationToken);
    }

    public static async Task<Packet?> ReceiveAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            stream);

        byte[] lengthBuffer =
            new byte[4];

        bool hasLength =
            await ReadExactlyAsync(
                stream,
                lengthBuffer,
                cancellationToken);

        if (!hasLength)
        {
            return null;
        }

        int length =
            BitConverter.ToInt32(
                lengthBuffer);

        if (length <= 0 ||
            length > MaximumPacketBytes)
        {
            throw new IOException(
                $"Invalid incoming packet length: {length}.");
        }

        byte[] data =
            new byte[length];

        bool hasPayload =
            await ReadExactlyAsync(
                stream,
                data,
                cancellationToken);

        if (!hasPayload)
        {
            return null;
        }

        return Deserialize(
            data);
    }

    private static async Task<bool> ReadExactlyAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        int offset =
            0;

        while (offset < buffer.Length)
        {
            int read =
                await stream.ReadAsync(
                    buffer[offset..],
                    cancellationToken);

            if (read == 0)
            {
                return false;
            }

            offset +=
                read;
        }

        return true;
    }
}
