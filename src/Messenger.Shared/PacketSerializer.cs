using System.Text;
using System.Text.Json;
using Messenger.Shared.Packets;

namespace Messenger.Shared.Network;

public static class PacketSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static byte[] Serialize(Packet packet)
    {
        var typeName = packet.GetType().Name;
        var json = JsonSerializer.Serialize(packet, packet.GetType(), Options);
        var payload = $"{typeName}\n{json}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var length = BitConverter.GetBytes(bytes.Length);
        return length.Concat(bytes).ToArray();
    }

    public static Packet? Deserialize(byte[] data)
    {
        var payload = Encoding.UTF8.GetString(data);
        var newlineIndex = payload.IndexOf('\n');
        if (newlineIndex < 0)
            return null;

        var typeName = payload[..newlineIndex];
        var json = payload[(newlineIndex + 1)..];

        var type = Type.GetType($"Messenger.Shared.Packets.{typeName}");
        if (type == null)
            return null;

        return JsonSerializer.Deserialize(json, type, Options) as Packet;
    }

    public static async Task SendAsync(Stream stream, Packet packet, CancellationToken cancellationToken = default)
    {
        var data = Serialize(packet);
        await stream.WriteAsync(data, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<Packet?> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var lengthBuffer = new byte[4];
        var read = await stream.ReadAsync(lengthBuffer, cancellationToken);
        if (read < 4)
            return null;

        var length = BitConverter.ToInt32(lengthBuffer);
        if (length <= 0 || length > 1024 * 1024)
            return null;

        var data = new byte[length];
        var totalRead = 0;
        while (totalRead < length)
        {
            var bytesRead = await stream.ReadAsync(data.AsMemory(totalRead, length - totalRead), cancellationToken);
            if (bytesRead == 0)
                return null;
            totalRead += bytesRead;
        }

        return Deserialize(data);
    }
}
