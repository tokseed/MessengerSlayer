. "$PSScriptRoot/Common.ps1"

Assert-Command "docker"

Push-Location $RepositoryRoot

try
{
    docker compose up -d sqlserver

    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }

    Wait-SqlContainer
}
finally
{
    Pop-Location
}
