using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Messenger.Server.Database;
using Messenger.Server.Network;

IConfigurationRoot configuration =
    new ConfigurationBuilder()
        .SetBasePath(
            Directory.GetCurrentDirectory())
        .AddJsonFile(
            "appsettings.json",
            optional: false)
        .Build();

string? connectionString =
    configuration.GetConnectionString(
        "DefaultConnection");

if (string.IsNullOrWhiteSpace(
        connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is missing.");
}

int port =
    configuration.GetValue<int>(
        "TcpServer:Port");

string certPath =
    configuration.GetValue<string>(
        "TcpServer:CertificatePath") ??
    "Certs/server.pfx";

string certPassword =
    configuration.GetValue<string>(
        "TcpServer:CertificatePassword") ??
    string.Empty;

X509Certificate2 certificate =
    X509CertificateLoader.LoadPkcs12FromFile(
        certPath,
        certPassword);

Console.WriteLine(
    $"Certificate loaded: {certificate.Subject}");

DbContextOptions<MessengerDbContext> dbContextOptions =
    new DbContextOptionsBuilder<MessengerDbContext>()
        .UseSqlServer(
            connectionString)
        .Options;

// Database initialization gets its own short-lived context.
// Runtime clients never share this instance.
await using (MessengerDbContext setupDb =
             new(dbContextOptions))
{
    await setupDb.Database.EnsureCreatedAsync();
}

TcpServer server =
    new(
        port,
        certificate,
        dbContextOptions);

using CancellationTokenSource cts =
    new();

Console.CancelKeyPress +=
    (_, eventArgs) =>
    {
        eventArgs.Cancel =
            true;

        cts.Cancel();
    };

try
{
    await server.StartAsync(
        cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine(
        "Server stopped.");
}
