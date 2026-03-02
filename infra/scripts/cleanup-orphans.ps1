#Requires -Version 5.1
<#
.SYNOPSIS
    Deletes orphaned resources in the TST environment.
.DESCRIPTION
    Lists and deletes orphaned App Service Plans and User-Assigned Managed Identities
    in the TST resource group. These resources are not attached to any active workload.
.EXAMPLE
    .\cleanup-orphans.ps1
#>

$ErrorActionPreference = "Stop"
$ResourceGroup = "rg-preflightapi-eastus-test"

Write-Host "=== TST Orphan Cleanup ===" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroup"
Write-Host ""

# --- Orphaned App Service Plans (no sites attached) ---

$OrphanASPs = @(
    "ASP-rgpreflightapieastustest-ad15"
    "ASP-rgpreflightapieastustest-9665"
)

Write-Host "--- Orphaned App Service Plans ---" -ForegroundColor Yellow
foreach ($asp in $OrphanASPs) {
    Write-Host "  $asp"
}
Write-Host ""

# --- Orphaned User-Assigned Managed Identities ---
# From earlier OIDC experiments (before switching to App Registrations)
# and previous app deployments. None are referenced by any active resource.

$OrphanMIs = @(
    "oidc-msi-890a"
    "oidc-msi-a339"
    "oidc-msi-93df"
    "oidc-msi-baf9"
    "preflightapi-eas-id-bb3b"
    "preflightapi-eas-id-b84e"
    "az-func-prefligh-id-ab89"
)

Write-Host "--- Orphaned User-Assigned Managed Identities ---" -ForegroundColor Yellow
foreach ($mi in $OrphanMIs) {
    Write-Host "  $mi"
}
Write-Host ""

# --- Confirm ---

$confirm = Read-Host "Delete all orphaned resources listed above? (y/N)"
if ($confirm -ne "y" -and $confirm -ne "Y") {
    Write-Host "Aborted."
    exit 0
}

Write-Host ""
Write-Host "Deleting orphaned App Service Plans..."
foreach ($asp in $OrphanASPs) {
    Write-Host "  Deleting $asp..."
    try {
        az appservice plan delete --name $asp --resource-group $ResourceGroup --yes 2>$null
        Write-Host "    Done." -ForegroundColor Green
    } catch {
        Write-Host "    Not found or already deleted." -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Deleting orphaned Managed Identities..."
foreach ($mi in $OrphanMIs) {
    Write-Host "  Deleting $mi..."
    try {
        az identity delete --name $mi --resource-group $ResourceGroup 2>$null
        Write-Host "    Done." -ForegroundColor Green
    } catch {
        Write-Host "    Not found or already deleted." -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Cleanup complete." -ForegroundColor Cyan
