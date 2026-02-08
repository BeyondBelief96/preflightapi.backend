#!/usr/bin/env bash
set -euo pipefail

# Creates an Entra ID App Registration with federated credentials for GitHub Actions OIDC,
# assigns Contributor role on the resource group, and pushes secrets to the GitHub repo.
#
# Prerequisites:
#   - Azure CLI logged in with permissions to create App Registrations and role assignments
#   - GitHub CLI (gh) authenticated
#
# Usage:
#   ./setup-github-oidc.sh --env prod --branch main --repo BeyondBelief96/Preflight.API

# ─── Parse arguments ────────────────────────────────────────────────────────────

ENV=""
BRANCH=""
REPO=""

while [[ $# -gt 0 ]]; do
  case $1 in
    --env)    ENV="$2"; shift 2 ;;
    --branch) BRANCH="$2"; shift 2 ;;
    --repo)   REPO="$2"; shift 2 ;;
    *) echo "Unknown argument: $1"; exit 1 ;;
  esac
done

if [[ -z "$ENV" || -z "$BRANCH" || -z "$REPO" ]]; then
  echo "Usage: $0 --env <test|prod> --branch <branch> --repo <owner/repo>"
  exit 1
fi

LOCATION="eastus"
RG_NAME="rg-preflightapi-${LOCATION}-${ENV}"
APP_NAME="github-deploy-preflightapi-${ENV}"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

echo "=== Configuration ==="
echo "Environment:    ${ENV}"
echo "Branch:         ${BRANCH}"
echo "Repo:           ${REPO}"
echo "Resource Group: ${RG_NAME}"
echo "App Name:       ${APP_NAME}"
echo "Subscription:   ${SUBSCRIPTION_ID}"
echo ""

# ─── Create App Registration ───────────────────────────────────────────────────

echo "Creating App Registration: ${APP_NAME}..."
APP_ID=$(az ad app create --display-name "${APP_NAME}" --query appId -o tsv)
echo "App (client) ID: ${APP_ID}"

# Create service principal
echo "Creating Service Principal..."
SP_OBJECT_ID=$(az ad sp create --id "${APP_ID}" --query id -o tsv 2>/dev/null || \
  az ad sp show --id "${APP_ID}" --query id -o tsv)
echo "SP Object ID: ${SP_OBJECT_ID}"

# ─── Add federated credential for GitHub Actions ───────────────────────────────

echo "Adding federated credential for branch '${BRANCH}'..."
CREDENTIAL_NAME="github-${ENV}-${BRANCH}"

az ad app federated-credential create \
  --id "${APP_ID}" \
  --parameters "{
    \"name\": \"${CREDENTIAL_NAME}\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${REPO}:ref:refs/heads/${BRANCH}\",
    \"audiences\": [\"api://AzureADTokenExchange\"],
    \"description\": \"GitHub Actions OIDC for ${REPO} (${ENV}, branch: ${BRANCH})\"
  }"

echo "Federated credential created: ${CREDENTIAL_NAME}"

# ─── Assign Contributor role on the resource group ──────────────────────────────

echo "Assigning Contributor role on ${RG_NAME}..."
az role assignment create \
  --assignee-object-id "${SP_OBJECT_ID}" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/${RG_NAME}" \
  --output none

echo "Role assigned."

# ─── Assign Contributor role on PostgreSQL server (for firewall management) ─────

DB_SERVER_NAME="pgsql-preflightapi-${LOCATION}-${ENV}"
echo "Assigning Contributor role on PostgreSQL server ${DB_SERVER_NAME}..."
az role assignment create \
  --assignee-object-id "${SP_OBJECT_ID}" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/${RG_NAME}/providers/Microsoft.DBforPostgreSQL/flexibleServers/${DB_SERVER_NAME}" \
  --output none 2>/dev/null || echo "  (PostgreSQL server may not exist yet — assign after infra deployment)"

echo "Role assigned."

# ─── Push secrets to GitHub ─────────────────────────────────────────────────────

echo ""
echo "Pushing secrets to GitHub repo: ${REPO}..."

ENV_UPPER=$(echo "${ENV}" | tr '[:lower:]' '[:upper:]')

gh secret set "AZURE_GITHUB_DEPLOYMENT_APP_REGISTRATION_CLIENTID" \
  --repo "${REPO}" --body "${APP_ID}"

gh secret set "AZURE_TENANTID" \
  --repo "${REPO}" --body "${TENANT_ID}"

gh secret set "AZURE_SUBSCRIPTIONID" \
  --repo "${REPO}" --body "${SUBSCRIPTION_ID}"

echo ""
echo "=== Done ==="
echo ""
echo "GitHub secrets set:"
echo "  AZURE_GITHUB_DEPLOYMENT_APP_REGISTRATION_CLIENTID = ${APP_ID}"
echo "  AZURE_TENANTID = ${TENANT_ID}"
echo "  AZURE_SUBSCRIPTIONID = ${SUBSCRIPTION_ID}"
echo ""
echo "Next steps:"
echo "  1. Deploy infrastructure: az deployment sub create --location ${LOCATION} --template-file infra/main.bicep --parameters infra/parameters/${ENV}.bicepparam"
echo "  2. Set DB_CONNECTION_STRING_${ENV_UPPER} secret in GitHub manually"
echo "  3. Configure any remaining app settings (NOAA, NMS, etc.) on the Web App"
echo "  4. SP Object ID for Bicep githubDeploymentPrincipalId param: ${SP_OBJECT_ID}"
