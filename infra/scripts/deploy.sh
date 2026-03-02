#!/usr/bin/env bash
set -euo pipefail

ENV="${1:?Usage: ./deploy.sh <test|prod>}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env.${ENV}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Error: $ENV_FILE not found. Copy from .env.example and fill in your secrets."
  exit 1
fi

# Export all variables from the env file
set -a
source "$ENV_FILE"
set +a

echo "Deploying infrastructure for environment: $ENV"

az deployment sub create \
  --location eastus \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters "$SCRIPT_DIR/parameters/${ENV}.bicepparam" \
  --name "preflight-${ENV}-$(date +%Y%m%d-%H%M%S)"
