. "$PSScriptRoot/Common.ps1"

Assert-Command "dotnet"

$rids =
    @(
        "win-x64",
        "win-arm64",
        "linux-x64",
        "linux-arm64",
        "osx-x64",
        "osx-arm64"
    )

$publishRoot =
    Join-Path `
        $RepositoryRoot `
        "publish"

if (Test-Path $publishRoot)
{
    Remove-Item `
        -Recurse `
        -Force `
        $publishRoot
}

New-Item `
    -ItemType Directory `
    -Path $publishRoot |
    Out-Null

Push-Location $RepositoryRoot

try
{
    dotnet restore "./src/MessengerSlayer.slnx"

    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }

    foreach ($rid in $rids)
    {
        Write-Host ""
        Write-Host "Publishing $rid..."

        $clientOutput =
            Join-Path `
                $publishRoot `
                "$rid/client"

        $serverOutput =
            Join-Path `
                $publishRoot `
                "$rid/server"

        dotnet publish "./src/Messenger.Client/Messenger.Client.csproj" `
            -c Release `
            -r $rid `
            --self-contained true `
            -o $clientOutput

        if ($LASTEXITCODE -ne 0)
        {
            exit $LASTEXITCODE
        }

        dotnet publish "./src/Messenger.Server/Messenger.Server.csproj" `
            -c Release `
            -r $rid `
            --self-contained true `
            -o $serverOutput

        if ($LASTEXITCODE -ne 0)
        {
            exit $LASTEXITCODE
        }
    }

    Write-Host ""
    Write-Host "Published to:"
    Write-Host "  $publishRoot"
}
finally
{
    Pop-Location
}
