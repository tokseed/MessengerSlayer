using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Messenger.Server.Database;
using Messenger.Server.Network;
using Messenger.Server.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
var port = configuration.GetValue<int>("TcpServer:Port");
var certPath = configuration.GetValue<string>("TcpServer:CertificatePath") ?? "Certs/server.pfx";
var certPassword = configuration.GetValue<string>("TcpServer:CertificatePassword") ?? "";

var certificate = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
Console.WriteLine($"Certificate loaded: {certificate.Subject}");

var dbContextOptions = new DbContextOptionsBuilder<MessengerDbContext>()
    .UseSqlServer(connectionString)
    .Options;

using var db = new MessengerDbContext(dbContextOptions);
await db.Database.EnsureCreatedAsync();

var authService = new AuthService(db);
var messageService = new MessageService(db);
var chatService = new ChatService(db);

var server = new TcpServer(port, certificate, authService, messageService, chatService);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await server.StartAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Server stopped.");
}
