# Infrastructure (Bicep) Guide

This guide explains how the Bicep infrastructure-as-code templates work, how to deploy them, and how to spin up a sandbox environment for testing.

## Directory Structure

```
infra/
├── main.bicep                          # Root orchestration (subscription-scoped)
├── main.json                           # Compiled ARM template (auto-generated)
├── .env.example                        # Template for secret environment variables
├── .env.test                           # TST secrets (gitignored)
├── .env.prod                           # PRD secrets (gitignored)
├── modules/
│   ├── monitoring.bicep                # Log Analytics + 2x Application Insights
│   ├── postgresql.bicep                # PostgreSQL Flexible Server + PostGIS
│   ├── storage.bicep                   # Blob storage (data + optional Functions storage)
│   ├── key-vault.bicep                 # Azure Key Vault
│   ├── app-service.bicep               # App Service Plan + Web App (Windows or Linux)
│   ├── function-app.bicep              # Flex Consumption Plan + Function App
│   ├── apim.bicep                      # API Management + products + policies
│   ├── role-assignments.bicep          # All RBAC role assignments
│   └── github-identity.bicep           # Standalone: user-assigned MI for OIDC (not wired into main)
├── parameters/
│   ├── test.bicepparam                 # TST parameter values (CLI deployments)
│   ├── prod.bicepparam                 # PRD parameter values (CLI deployments)
│   ├── test.parameters.json            # TST parameter values (VS Code deployments)
│   └── prod.parameters.json            # PRD parameter values (VS Code deployments)
└── scripts/
    ├── deploy.ps1                      # PowerShell deploy script
    ├── deploy.sh                       # Bash deploy script
    ├── cleanup-orphans.ps1             # PowerShell orphan cleanup (TST)
    ├── cleanup-orphans.sh              # Bash orphan cleanup (TST)
    └── setup-github-oidc.sh            # Creates App Registration + OIDC for GitHub Actions
```

## How It Works

### Architecture

`main.bicep` is a **subscription-scoped** deployment. It first creates the resource group, then deploys 8 modules into it:

```
main.bicep (subscription scope)
  ├── Resource Group
  ├── monitoring        → Log Analytics workspace + 2 App Insights (API + Functions)
  ├── postgresql        → Flexible Server + database + PostGIS + firewall rules
  ├── storage           → Data storage account + blob containers + optional Functions storage
  ├── key-vault         → Key Vault with RBAC authorization
  ├── app-service       → App Service Plan + Web App (configurable Windows/Linux)
  ├── function-app      → Flex Consumption Plan + Function App + all cron job settings
  ├── apim              → API Management + products + policies
  └── role-assignments  → RBAC for managed identities, GitHub SP, and APIM SP
```

### Module Dependencies

Modules reference each other's outputs, creating an implicit dependency graph:

```
monitoring ──────────────────┬──→ app-service (API App Insights connection string)
                             └──→ function-app (Functions App Insights connection string)
postgresql ──────────────────┬──→ app-service (server FQDN)
                             └──→ function-app (server FQDN)
storage ─────────────────────┬──→ app-service (storage account name, container names)
                             ├──→ function-app (storage account name, container names)
                             └──→ role-assignments (storage account name)
key-vault ───────────────────┬──→ function-app (Key Vault name for certificate renewal)
                             └──→ role-assignments (Key Vault name)
app-service ─────────────────┬──→ function-app (web app hostname for health endpoint URL)
                             ├──→ apim (backend hostname)
                             └──→ role-assignments (web app principal ID)
function-app ────────────────────→ role-assignments (function app principal ID)
apim ────────────────────────────→ role-assignments (APIM name)
```

### Parameterization

Every resource name is fully parameterized — nothing is derived or auto-generated. This ensures Bicep matches exactly what exists in Azure. Each environment has its own parameter file with the exact deployed resource names.

Secrets are loaded from `.env.*` files via `readEnvironmentVariable()` in `.bicepparam` files (CLI), or entered interactively via VS Code.

### Environments

| | TST | PRD |
|---|---|---|
| Web App platform | Windows (F1/Free) | Linux (B2/Basic) |
| PostgreSQL version | 15 | 16 |
| Functions storage | Shared (single account) | Dedicated (separate account) |
| Storage SKU | Standard_RAGRS | Standard_RAGRS |
| APIM SKU | Developer | BasicV2 |
| Resend alerts | Disabled | Enabled |

## Prerequisites

1. **Azure CLI** installed and logged in:
   ```powershell
   az login
   az account set --subscription <subscription-id>
   ```

2. **Bicep CLI** (bundled with Azure CLI, or install separately):
   ```powershell
   az bicep install
   az bicep version
   ```

3. **Secrets file** — copy `.env.example` to `.env.test` or `.env.prod` and fill in values.

## Deploying

### Option 1: PowerShell Script (recommended)

```powershell
cd infra

# Deploy TST
.\scripts\deploy.ps1 test

# Deploy PRD
.\scripts\deploy.ps1 prod
```

The script loads secrets from `.env.<environment>`, then runs `az deployment sub create`.

### Option 2: VS Code Bicep Extension

1. Install the [Bicep extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-bicep)
2. Open `infra/main.bicep`
3. Right-click → **Deploy Bicep File...**
4. Scope: **Subscription**, Location: **East US**
5. Select `parameters/test.parameters.json` or `parameters/prod.parameters.json`
6. VS Code prompts you for any empty `@secure()` parameters (passwords, API keys, etc.)

### Option 3: Azure CLI Directly

```powershell
# Load secrets into environment
Get-Content infra/.env.test | ForEach-Object {
    $line = $_.Trim()
    if ($line -and -not $line.StartsWith("#")) {
        $k, $v = $line -split "=", 2
        [Environment]::SetEnvironmentVariable($k.Trim(), $v.Trim(), "Process")
    }
}

# Deploy
az deployment sub create `
    --location eastus `
    --template-file infra/main.bicep `
    --parameters infra/parameters/test.bicepparam
```

## Validating Without Deploying (What-If)

Before deploying, preview what changes Bicep would make:

```powershell
# Load secrets first (same as above), then:

# TST what-if
az deployment sub what-if `
    --location eastus `
    --template-file infra/main.bicep `
    --parameters infra/parameters/test.bicepparam

# PRD what-if (switch subscription first)
az account set --subscription <prd-subscription-id>
az deployment sub what-if `
    --location eastus `
    --template-file infra/main.bicep `
    --parameters infra/parameters/prod.bicepparam
```

The output shows resources that would be created, modified, or deleted. For an aligned deployment, you should see mostly **NoChange** with minor **Modify** entries.

## Spinning Up a Sandbox Environment

To create a throwaway copy of the infrastructure for testing Bicep changes without affecting TST or PRD:

### Step 1: Create a Sandbox Parameter File

Copy the test parameters and change all resource names to be unique:

```powershell
Copy-Item infra/parameters/test.parameters.json infra/parameters/sandbox.parameters.json
```

Edit `sandbox.parameters.json` and change these values (all resource names must be globally or regionally unique):

```json
{
    "resourceGroupName":              { "value": "rg-preflightapi-eastus-sandbox" },
    "logAnalyticsName":               { "value": "log-preflightapi-sandbox" },
    "apiAppInsightsName":             { "value": "appi-preflightapi-api-sandbox" },
    "functionAppInsightsName":        { "value": "appi-preflightapi-func-sandbox" },
    "postgresServerName":             { "value": "pgsql-preflightapi-sandbox" },
    "storageAccountName":             { "value": "stpreflightapisandbox" },
    "appServicePlanName":             { "value": "asp-preflightapi-api-sandbox" },
    "webAppName":                     { "value": "preflightapi-web-sandbox" },
    "functionsPlanName":              { "value": "asp-preflightapi-func-sandbox" },
    "functionAppName":                { "value": "preflightapi-func-sandbox" },
    "keyVaultName":                   { "value": "kv-preflightapi-sandbox" },
    "apimServiceName":                { "value": "apim-preflightapi-sandbox" },
    "functionsStorageName":           { "value": "" },
    "terminalProceduresContainerName": { "value": "terminal-procedures" },
    "chartSupplementsContainerName":  { "value": "chart-supplements" },
    "preflightApiResourcesContainerName": { "value": "preflightapi-resources" }
}
```

> **Important:** Storage account names must be 3-24 lowercase alphanumeric characters, globally unique. Web app and function app names must be globally unique. Key Vault names must be globally unique.

### Step 2: Preview Changes

```powershell
az deployment sub what-if `
    --location eastus `
    --template-file infra/main.bicep `
    --parameters infra/parameters/sandbox.parameters.json `
    --parameters dbAdminPassword="TempPassword123!" `
    --parameters noaaApiKey="placeholder" `
    --parameters gatewaySecret="placeholder" `
    --parameters nmsBaseUrl="https://example.com" `
    --parameters nmsAuthBaseUrl="https://example.com" `
    --parameters nmsClientId="placeholder" `
    --parameters nmsClientSecret="placeholder" `
    --parameters apimPublisherEmail="you@example.com"
```

### Step 3: Deploy the Sandbox

```powershell
az deployment sub create `
    --location eastus `
    --template-file infra/main.bicep `
    --parameters infra/parameters/sandbox.parameters.json `
    --parameters dbAdminPassword="TempPassword123!" `
    --parameters noaaApiKey="placeholder" `
    --parameters gatewaySecret="placeholder" `
    --parameters nmsBaseUrl="https://example.com" `
    --parameters nmsAuthBaseUrl="https://example.com" `
    --parameters nmsClientId="placeholder" `
    --parameters nmsClientSecret="placeholder" `
    --parameters apimPublisherEmail="you@example.com" `
    --name "preflight-sandbox-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
```

> **Note:** APIM (API Management) takes 15-30+ minutes to provision. If you don't need it, you can comment out the `apim` and its reference in `roleAssignments` modules in a local copy of `main.bicep` to speed things up.

### Step 4: Verify It Worked

```powershell
# List all resources in the sandbox resource group
az resource list --resource-group rg-preflightapi-eastus-sandbox --output table

# Check the web app is running
az webapp show --name preflightapi-web-sandbox --resource-group rg-preflightapi-eastus-sandbox --query "state" -o tsv
```

### Step 5: Delete the Sandbox

Delete the entire resource group — this removes everything inside it:

```powershell
az group delete --name rg-preflightapi-eastus-sandbox --yes --no-wait
```

The `--no-wait` flag returns immediately while deletion continues in the background. Full cleanup takes a few minutes.

To verify it's gone:

```powershell
az group show --name rg-preflightapi-eastus-sandbox 2>$null
# Should return an error if successfully deleted
```

> **Key Vault soft-delete:** Deleted Key Vaults are retained for 90 days in a soft-deleted state. If you try to redeploy with the same Key Vault name, you'll need to purge the old one first:
> ```powershell
> az keyvault purge --name kv-preflightapi-sandbox --location eastus
> ```

### Cleanup Checklist

After deleting the sandbox, verify no costs linger:

- [ ] Resource group deleted: `az group show --name rg-preflightapi-eastus-sandbox` returns error
- [ ] No orphaned resources: check [Azure Portal > All Resources](https://portal.azure.com/#view/HubsExtension/BrowseAll) filtered by "sandbox"
- [ ] Key Vault purged (if redeploying): `az keyvault purge --name kv-preflightapi-sandbox --location eastus`

## RBAC Role Assignments

The `role-assignments` module creates these assignments:

| Principal | Role | Scope | Purpose |
|---|---|---|---|
| Web App MI | Storage Blob Data Contributor | Storage account | Read/write blob data without keys |
| Function App MI | Storage Blob Data Contributor | Storage account | Read/write blob data without keys |
| Function App MI | Key Vault Secrets User | Key Vault | Read secrets for cert renewal |
| GitHub deploy SP | Contributor | Resource Group | Deploy apps, manage resources |
| GitHub deploy SP | Contributor | PostgreSQL server | Manage firewall rules in CI/CD |
| GitHub deploy SP | API Mgmt Service Contributor | APIM | Deploy API policies in CI/CD |
| APIM SP (optional) | API Mgmt Service Contributor | APIM | Frontend subscription management |
| APIM SP (optional) | Log Analytics Reader | Log Analytics | Frontend analytics dashboard |

The GitHub SP and APIM SP assignments are conditional — they're only created when the corresponding principal ID parameters are provided.

## Adding New App Settings

To add a new app setting to the Web App or Function App:

1. Add the `param` to the appropriate module (`app-service.bicep` or `function-app.bicep`)
2. Add the setting to the `appSettings` array (or a conditional settings array like `resendSettings`)
3. Add the `param` to `main.bicep` and wire it to the module call
4. Add the value to both `.bicepparam` files and both `.parameters.json` files
5. If it's a secret, add it to `.env.example` and use `readEnvironmentVariable()` in `.bicepparam`

## Troubleshooting

### "The subscription is not registered to use namespace 'Microsoft.XXX'"

Register the resource provider:
```powershell
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.DBforPostgreSQL
az provider register --namespace Microsoft.ApiManagement
```

### Deployment fails with "Conflict" on existing resources

Bicep deployments are idempotent — rerunning should work. If a resource was modified outside of Bicep (manually in the portal), you may get conflicts. Use `what-if` to see what Bicep wants to change, then decide whether to accept or adjust the parameter values.

### Key Vault name already taken

Key Vault names are globally unique and soft-deleted vaults persist for 90 days. Purge or choose a different name:
```powershell
az keyvault list-deleted --query "[].name" -o tsv
az keyvault purge --name <vault-name> --location eastus
```

### APIM deployment is very slow

APIM provisioning can take 30-45 minutes, especially for initial creation. This is normal for the Azure API Management service.
