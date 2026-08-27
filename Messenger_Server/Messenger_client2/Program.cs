using System.Net.Sockets;
using Mesenger_common;

using var client = new TcpClient();
try
{
    await client.ConnectAsync("127.0.0.1", 8888);
    Console.WriteLine("Подключено к серверу.");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка подключения: {ex.Message}");
    return;
}

using var stream = client.GetStream();
using var reader = new StreamReader(stream);
using var writer = new StreamWriter(stream) { AutoFlush = true };

// 1. АВТОРИЗАЦИЯ НА СЕРВЕРЕ
Console.Write("Введите ваше имя/логин: ");
string username = Console.ReadLine() ?? "User";

var authMsg = new NetworkMessage
{
    Type = MessageType.Auth,
    Sender = username
};

await writer.WriteLineAsync(authMsg.Serialize());

// 2. ФОНОВОЕ ЧТЕНИЕ ОТВЕТОВ И ВХОДЯЩИХ СООБЩЕНИЙ
_ = Task.Run(async () =>
{
    try
    {
        while (true)
        {
            string? responseJson = await reader.ReadLineAsync();
            if (responseJson == null) break;

            var incoming = NetworkMessage.Deserialize(responseJson);
            if (incoming != null)
            {
                Console.WriteLine($"\n[{incoming.Sender}]: {incoming.Content}\n> ");
            }
        }
    }
    catch
    {
        Console.WriteLine("\nСоединение с сервером разорвано.");
    }
});

// 3. ЦИКЛ ОТПРАВКИ СООБЩЕНИЙ (Формат: <Получатель> <Текст>)
Console.WriteLine("\nОтправляйте ЛС в формате: <ИмяПолучателя> <Текст>");
Console.WriteLine("Пример: Anton Привет, как дела?");

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? string.Empty;
    if (string.IsNullOrWhiteSpace(input)) continue;

    var parts = input.Split(' ', 2);
    if (parts.Length < 2)
    {
        Console.WriteLine("Ошибка! Нужно указать получателя и текст через пробел.");
        continue;
    }

    string targetUser = parts[0];
    string messageText = parts[1];

    var directMsg = new NetworkMessage
    {
        Type = MessageType.DirectMessage,
        Sender = username,
        Target = targetUser,
        Content = messageText
    };

    await writer.WriteLineAsync(directMsg.Serialize());
}