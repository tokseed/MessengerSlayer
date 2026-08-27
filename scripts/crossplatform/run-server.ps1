. "$PSScriptRoot/Common.ps1"

Assert-Command "dotnet"

$password =
    Get-DotEnvValue `
        -Name "MSSQL_SA_PASSWORD" `
        -DefaultValue "Your_password123"

$databasePort =
    Get-DotEnvValue `
        -Name "MSSQL_HOST_PORT" `
        -DefaultValue "1433"

$database =
    Get-DotEnvValue `
        -Name "MESSENGER_DATABASE" `
        -DefaultValue "MessengerSlayer"

$serverPort =
    Get-DotEnvValue `
        -Name "MESSENGER_SERVER_PORT" `
        -DefaultValue "5000"

$env:ConnectionStrings__DefaultConnection =
    "Server=localhost,$databasePort;Database=$database;User Id=sa;Password=$password;TrustServerCertificate=True;"

$env:TcpServer__Port =
    $serverPort

$serverDirectory =
    Join-Path `
        $RepositoryRoot `
        "src/Messenger.Server"

Push-Location $serverDirectory

try
{
    Write-Host "Starting colleague Messenger.Server"
    Write-Host "Database: localhost:$databasePort / $database"
    Write-Host "TCP server port: $serverPort"
    Write-Host ""

    dotnet run
    exit $LASTEXITCODE
}
finally
{
    Pop-Location
}
