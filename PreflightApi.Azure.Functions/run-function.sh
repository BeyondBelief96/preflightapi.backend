#!/usr/bin/env bash
# Run a single Azure Function locally in isolation.
# Usage: ./run-function.sh MetarFunction
#        ./run-function.sh Metar          (auto-appends "Function" if missing)
#        ./run-function.sh                (lists all available functions)

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

FUNCTIONS=(
    AirportFunction
    AirspaceFunction
    CertificateRenewalFunction
    ChartSupplementFunction
    DataCurrencyAlertFunction
    FrequencyFunction
    GAirmetFunction
    MetarFunction
    NavaidFunction
    NotamDeltaSyncFunction
    NotamInitialLoadFunction
    ObstacleDailyChangeFunction
    ObstacleFunction
    PirepFunction
    ServiceOutageAlertFunction
    SigmetFunction
    SpecialUseAirspaceFunction
    TafFunction
    TerminalProcedureFunction
)

if [ -z "$1" ]; then
    echo "Available functions:"
    for fn in "${FUNCTIONS[@]}"; do
        echo "  $fn"
    done
    echo ""
    echo "Usage: $0 <FunctionName>"
    echo "  e.g. $0 MetarFunction"
    echo "  e.g. $0 Metar          (auto-appends 'Function')"
    exit 0
fi

NAME="$1"
# Auto-append "Function" if not already present
if [[ "$NAME" != *Function ]]; then
    NAME="${NAME}Function"
fi

# Validate the function name
FOUND=false
for fn in "${FUNCTIONS[@]}"; do
    if [ "$fn" = "$NAME" ]; then
        FOUND=true
        break
    fi
done

if [ "$FOUND" = false ]; then
    echo "Error: '$NAME' is not a known function."
    echo "Run '$0' with no arguments to see available functions."
    exit 1
fi

echo "Starting $NAME in isolation..."
cd "$SCRIPT_DIR" && func start --functions "$NAME"
