using System.Text.Json;

namespace Mesenger_common;

public enum MessageType
{
    Auth,           // Авторизация
    DirectMessage,  // Личное сообщение
    GroupMessage,   // Сообщение в группу
    CreateGroup,    // Создание группы
    FileTransfer    // Передача файла
}

public class NetworkMessage
{
    public MessageType Type { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty; // Receiver или GroupId
    public string Content { get; set; } = string.Empty; // Текст или название файла
    public byte[]? Payload { get; set; } // Данные файла

    public string Serialize() => JsonSerializer.Serialize(this);
    public static NetworkMessage? Deserialize(string json) => JsonSerializer.Deserialize<NetworkMessage>(json);
}