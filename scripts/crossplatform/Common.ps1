$ErrorActionPreference = "Stop"

$ScriptRoot =
    Split-Path `
        -Parent `
        $MyInvocation.MyCommand.Path

$RepositoryRoot =
    (Resolve-Path (
        Join-Path `
            $ScriptRoot `
            "../.."
    )).Path

function Get-DotEnvValue
{
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [string] $DefaultValue
    )

    $envFile =
        Join-Path `
            $RepositoryRoot `
            ".env"

    if (-not (Test-Path $envFile))
    {
        return $DefaultValue
    }

    $prefix =
        "$Name="

    $line =
        Get-Content $envFile |
        Where-Object {
            $trimmed =
                $_.Trim()

            -not $trimmed.StartsWith("#") -and
            $trimmed.StartsWith(
                $prefix,
                [System.StringComparison]::Ordinal
            )
        } |
        Select-Object -Last 1

    if ([string]::IsNullOrWhiteSpace($line))
    {
        return $DefaultValue
    }

    $value =
        $line.Substring(
            $line.IndexOf("=") + 1
        ).Trim()

    if (
        $value.Length -ge 2 -and
        (
            ($value.StartsWith('"') -and $value.EndsWith('"')) -or
            ($value.StartsWith("'") -and $value.EndsWith("'"))
        )
    )
    {
        $value =
            $value.Substring(
                1,
                $value.Length - 2
            )
    }

    return $value
}

function Assert-Command
{
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue))
    {
        throw "Required command '$Name' was not found."
    }
}

function Wait-SqlContainer
{
    param(
        [int] $TimeoutSeconds = 90
    )

    $deadline =
        (Get-Date).AddSeconds(
            $TimeoutSeconds
        )

    Write-Host "Waiting for SQL Server container..."

    while ((Get-Date) -lt $deadline)
    {
        $logs =
            docker logs messengerslayer-sql 2>&1 |
            Out-String

        if (
            $logs -match
            "SQL Server is now ready for client connections"
        )
        {
            Write-Host "SQL Server is ready."
            return
        }

        Start-Sleep -Seconds 2
    }

    Write-Host ""
    Write-Host "SQL Server did not report readiness within $TimeoutSeconds seconds."
    Write-Host "Container status:"
    docker compose `
        -f (Join-Path $RepositoryRoot "docker-compose.yml") `
        ps

    throw "SQL Server startup timeout."
}
