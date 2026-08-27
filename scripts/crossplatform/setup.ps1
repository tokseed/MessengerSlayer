. "$PSScriptRoot/Common.ps1"

Assert-Command "docker"
Assert-Command "dotnet"

& "$PSScriptRoot/db-up.ps1"

if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

& "$PSScriptRoot/build.ps1"
exit $LASTEXITCODE
