using Microsoft.Extensions.Configuration;
using Messenger.Client.Network;
using Messenger.Client.Services;
using Messenger.Shared.Packets;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var host = configuration.GetValue<string>("Server:Host") ?? "localhost";
var port = configuration.GetValue<int>("Server:Port");

using var client = new TcpClientService();

try
{
    await client.ConnectAsync(host, port);
    Console.WriteLine($"Connected to server at {host}:{port}");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to connect: {ex.Message}");
    return;
}

var authService = new AuthService(client);
var chatService = new ChatService(client);
var messageService = new MessageService(client);

int? currentUserId = null;

client.OnPacketReceived += packet =>
{
    if (packet is MessagePacket message && message.SenderId != currentUserId)
    {
        Console.WriteLine($"\n[Message from user {message.SenderId} in chat {message.ChatId}]: {message.Content}");
        Console.Write("> ");
    }
};

Console.WriteLine("Commands: register, login, users, chats, create-chat, send, history, exit");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrEmpty(input) || input == "exit")
        break;

    try
    {
        switch (input)
        {
            case "register":
            {
                Console.Write("Username: ");
                var username = Console.ReadLine() ?? "";
                Console.Write("Password: ");
                var password = Console.ReadLine() ?? "";
                Console.Write("First Name: ");
                var firstName = Console.ReadLine() ?? "";
                Console.Write("Last Name: ");
                var lastName = Console.ReadLine() ?? "";
                Console.Write("Phone: ");
                var phone = Console.ReadLine() ?? "";

                var result = await authService.RegisterAsync(username, password, firstName, lastName, phone);
                Console.WriteLine(result.Success
                    ? $"Registered! Your ID: {result.UserId}"
                    : $"Error: {result.Error}");
                break;
            }
            case "login":
            {
                Console.Write("Username: ");
                var username = Console.ReadLine() ?? "";
                Console.Write("Password: ");
                var password = Console.ReadLine() ?? "";

                var result = await authService.LoginAsync(username, password);
                if (result.Success)
                {
                    currentUserId = result.UserId;
                    Console.WriteLine($"Logged in! Your ID: {result.UserId}");
                }
                else
                {
                    Console.WriteLine($"Error: {result.Error}");
                }
                break;
            }
            case "users":
            {
                var users = await authService.GetUsersAsync();
                foreach (var user in users)
                {
                    Console.WriteLine($"  [{user.Id}] {user.FirstName} {user.LastName} ({user.Username})");
                }
                break;
            }
            case "chats":
            {
                if (currentUserId == null) { Console.WriteLine("Login first."); break; }
                var chats = await chatService.GetChatsAsync(currentUserId.Value);
                foreach (var chat in chats)
                {
                    Console.WriteLine($"  [{chat.Id}] {chat.Title} ({chat.ChatType})");
                }
                break;
            }
            case "create-chat":
            {
                if (currentUserId == null) { Console.WriteLine("Login first."); break; }
                Console.Write("Title: ");
                var title = Console.ReadLine() ?? "";
                Console.Write("Type (direct/group/channel): ");
                var chatType = Console.ReadLine() ?? "direct";
                Console.Write("Participant IDs (comma separated): ");
                var idsInput = Console.ReadLine() ?? "";
                var participantIds = idsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse).ToList();

                var result = await chatService.CreateAsync(title, chatType, participantIds);
                Console.WriteLine(result.Success
                    ? $"Chat created! ID: {result.ChatId}"
                    : $"Error: {result.Error}");
                break;
            }
            case "send":
            {
                if (currentUserId == null) { Console.WriteLine("Login first."); break; }
                Console.Write("Chat ID: ");
                var chatId = int.Parse(Console.ReadLine() ?? "0");
                Console.Write("Message: ");
                var content = Console.ReadLine() ?? "";

                var result = await messageService.SendAsync(chatId, content);
                Console.WriteLine(result.Success
                    ? $"Sent! Message ID: {result.MessageId}"
                    : $"Error: {result.Error}");
                break;
            }
            case "history":
            {
                Console.Write("Chat ID: ");
                var chatId = int.Parse(Console.ReadLine() ?? "0");
                var messages = await chatService.GetHistoryAsync(chatId);
                foreach (var msg in messages)
                {
                    Console.WriteLine($"  [{msg.SenderId}] {msg.Content} ({msg.SentAt:HH:mm:ss})");
                }
                break;
            }
            default:
                Console.WriteLine("Unknown command.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

client.Disconnect();
Console.WriteLine("Disconnected.");
