using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Mesenger_common;

ServerObject server = new ServerObject();
await server.ListenAsync();

class ServerObject
{
    private readonly TcpListener _tcpListener = new(IPAddress.Any, 8888);
    internal readonly ConcurrentDictionary<string, ClientObject> Clients = new();
    internal readonly ConcurrentDictionary<string, HashSet<string>> Groups = new();

    public async Task ListenAsync()
    {
        try
        {
            _tcpListener.Start();
            Console.WriteLine("[СЕРВЕР] Запущен на порту 8888. Ожидание подключений...");

            while (true)
            {
                TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
                ClientObject clientObject = new ClientObject(tcpClient, this);
                _ = Task.Run(clientObject.ProcessAsync);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА СЕРВЕРА] {ex.Message}");
        }
    }

    public async Task SendToClientAsync(string targetUsername, NetworkMessage message)
    {
        var targetClient = Clients.Values.FirstOrDefault(c => c.UserName == targetUsername);
        if (targetClient != null)
        {
            await targetClient.SendMessageAsync(message);
        }
        else
        {
            Console.WriteLine($"[СЕРВЕР] Получатель '{targetUsername}' не найден в сети.");
        }
    }

    public async Task BroadcastToGroupAsync(string groupId, NetworkMessage message)
    {
        if (Groups.TryGetValue(groupId, out var memberUsernames))
        {
            foreach (var client in Clients.Values)
            {
                if (memberUsernames.Contains(client.UserName) && client.UserName != message.Sender)
                {
                    await client.SendMessageAsync(message);
                }
            }
        }
    }

    public void RemoveConnection(string id)
    {
        if (Clients.TryRemove(id, out var client))
        {
            client.Close();
            if (!string.IsNullOrEmpty(client.UserName))
            {
                Console.WriteLine($"[СЕРВЕР] Пользователь {client.UserName} отключился.");
            }
        }
    }
}

class ClientObject
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string UserName { get; private set; } = string.Empty;

    private readonly TcpClient _client;
    private readonly ServerObject _server;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    public ClientObject(TcpClient tcpClient, ServerObject serverObject)
    {
        _client = tcpClient;
        _server = serverObject;
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public async Task ProcessAsync()
    {
        try
        {
            while (true)
            {
                string? rawJson = await _reader.ReadLineAsync();
                if (rawJson == null) break;

                var msg = NetworkMessage.Deserialize(rawJson);
                if (msg == null) continue;

                await HandleMessageAsync(msg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА КЛИЕНТА {UserName}] {ex.Message}");
        }
        finally
        {
            _server.RemoveConnection(Id);
        }
    }

    private async Task HandleMessageAsync(NetworkMessage msg)
    {
        switch (msg.Type)
        {
            case MessageType.Auth:
                UserName = msg.Sender;
                // Регистрируем клиента в словаре сервера по его уникальному ID
                _server.Clients[Id] = this;
                Console.WriteLine($"[СЕРВЕР] Авторизован: {UserName}");
                break;

            case MessageType.DirectMessage:
                Console.WriteLine($"[ЛС] {msg.Sender} -> {msg.Target}: {msg.Content}");
                await _server.SendToClientAsync(msg.Target, msg);
                break;

            case MessageType.CreateGroup:
                string[] members = msg.Content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                HashSet<string> groupMembers = new HashSet<string>(members) { msg.Sender };
                _server.Groups[msg.Target] = groupMembers;
                Console.WriteLine($"[ГРУППА] Создана {msg.Target} ({string.Join(", ", groupMembers)})");
                break;

            case MessageType.GroupMessage:
                Console.WriteLine($"[Группа {msg.Target}] {msg.Sender}: {msg.Content}");
                await _server.BroadcastToGroupAsync(msg.Target, msg);
                break;

            case MessageType.FileTransfer:
                int bytesCount = msg.Payload?.Length ?? 0;
                Console.WriteLine($"[ФАЙЛ] {msg.Sender} -> {msg.Target}: {msg.Content} ({bytesCount} байт)");
                await _server.SendToClientAsync(msg.Target, msg);
                break;
        }
    }

    public async Task SendMessageAsync(NetworkMessage message)
    {
        try
        {
            string json = message.Serialize();
            await _writer.WriteLineAsync(json.AsMemory());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА ОТПРАВКИ] Не удалось отправить сообщение клиенту {UserName}: {ex.Message}");
        }
    }

    public void Close()
    {
        try
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _client?.Dispose();
        }
        catch { }
    }
}