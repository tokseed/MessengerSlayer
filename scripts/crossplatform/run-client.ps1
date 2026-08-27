. "$PSScriptRoot/Common.ps1"

Assert-Command "dotnet"

Push-Location $RepositoryRoot

try
{
    dotnet run `
        --project "./src/Messenger.Client/Messenger.Client.csproj"

    exit $LASTEXITCODE
}
finally
{
    Pop-Location
}
