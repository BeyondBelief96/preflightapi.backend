# Run a single Azure Function locally in isolation.
# Usage: .\run-function.ps1 MetarFunction
#        .\run-function.ps1 Metar          (auto-appends "Function" if missing)
#        .\run-function.ps1                (lists all available functions)

param(
    [string]$Name
)

$Functions = @(
    "AirportFunction"
    "AirspaceFunction"
    "CertificateRenewalFunction"
    "ChartSupplementFunction"
    "DataCurrencyAlertFunction"
    "FrequencyFunction"
    "GAirmetFunction"
    "MetarFunction"
    "NavaidFunction"
    "NotamDeltaSyncFunction"
    "NotamInitialLoadFunction"
    "ObstacleDailyChangeFunction"
    "ObstacleFunction"
    "PirepFunction"
    "ServiceOutageAlertFunction"
    "SigmetFunction"
    "SpecialUseAirspaceFunction"
    "TafFunction"
    "TerminalProcedureFunction"
)

if (-not $Name) {
    Write-Host "Available functions:"
    foreach ($fn in $Functions) {
        Write-Host "  $fn"
    }
    Write-Host ""
    Write-Host "Usage: .\run-function.ps1 <FunctionName>"
    Write-Host "  e.g. .\run-function.ps1 MetarFunction"
    Write-Host "  e.g. .\run-function.ps1 Metar          (auto-appends 'Function')"
    exit 0
}

# Auto-append "Function" if not already present
if ($Name -notlike "*Function") {
    $Name = "${Name}Function"
}

if ($Name -notin $Functions) {
    Write-Host "Error: '$Name' is not a known function." -ForegroundColor Red
    Write-Host "Run '.\run-function.ps1' with no arguments to see available functions."
    exit 1
}

Write-Host "Starting $Name in isolation..." -ForegroundColor Green
Push-Location $PSScriptRoot
try {
    func start --functions $Name
}
finally {
    Pop-Location
}
