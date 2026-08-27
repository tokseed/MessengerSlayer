. "$PSScriptRoot/Common.ps1"

Assert-Command "dotnet"

Push-Location $RepositoryRoot

try
{
    Write-Host "[1/3] Restore"
    dotnet restore "./src/MessengerSlayer.slnx"

    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "[2/3] Debug"
    dotnet build "./src/MessengerSlayer.slnx" `
        -c Debug `
        --no-restore

    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "[3/3] Release"
    dotnet build "./src/MessengerSlayer.slnx" `
        -c Release `
        --no-restore

    exit $LASTEXITCODE
}
finally
{
    Pop-Location
}
