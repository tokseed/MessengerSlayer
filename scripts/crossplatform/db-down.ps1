. "$PSScriptRoot/Common.ps1"

Assert-Command "docker"

Push-Location $RepositoryRoot

try
{
    docker compose down
    exit $LASTEXITCODE
}
finally
{
    Pop-Location
}
