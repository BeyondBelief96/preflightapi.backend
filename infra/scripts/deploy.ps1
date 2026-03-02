#Requires -Version 5.1
<#
.SYNOPSIS
    Deploys Bicep infrastructure for the specified environment.
.DESCRIPTION
    Loads secrets from .env.<environment> file and runs az deployment sub create
    with the corresponding parameter file.
.PARAMETER Environment
    Target environment: test or prod
.EXAMPLE
    .\deploy.ps1 test
    .\deploy.ps1 prod
#>

param(
    [Parameter(Mandatory, Position = 0)]
    [ValidateSet("test", "prod")]
    [string]$Environment
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$EnvFile = Join-Path $ScriptDir ".env.$Environment"

if (-not (Test-Path $EnvFile)) {
    Write-Error "Error: $EnvFile not found. Copy from .env.example and fill in your secrets."
    exit 1
}

# Export all variables from the env file
Get-Content $EnvFile | ForEach-Object {
    $line = $_.Trim()
    # Skip comments and blank lines
    if ($line -and -not $line.StartsWith("#")) {
        $parts = $line -split "=", 2
        if ($parts.Count -eq 2) {
            [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
        }
    }
}

$deploymentName = "preflight-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

Write-Host "Deploying infrastructure for environment: $Environment" -ForegroundColor Cyan
Write-Host "Deployment name: $deploymentName"
Write-Host ""

az deployment sub create `
    --location eastus `
    --template-file "$ScriptDir/main.bicep" `
    --parameters "$ScriptDir/parameters/$Environment.bicepparam" `
    --name $deploymentName

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Deployment complete." -ForegroundColor Green
