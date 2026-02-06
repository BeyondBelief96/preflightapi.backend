# Infrastructure Setup Guide

Reference for provisioning Azure environments for PreflightApi. Based on the test environment setup (February 2026).

## Azure Resources Required

| Resource | Test Name | Type |
|----------|-----------|------|
| Resource Group | `rg-preflightapi-eastus-test` | Container for all resources |
| PostgreSQL Flexible Server | `pgsql-preflightapi-eastus-test` | Database |
| Storage Account | `rgpreflightapieastusad` | Blob storage for diagrams/charts |
| App Service (Web API) | `preflightapi-eastus-web-api-test` | ASP.NET Core 8 on Linux |
| Function App | `preflightapi-eastus-function-app-test` | .NET 8 Isolated, Flex Consumption |

## Step-by-Step Setup

### 1. Resource Group

Create a resource group in your target region (e.g., East US).

### 2. PostgreSQL Flexible Server

- Create an Azure Database for PostgreSQL Flexible Server
- **Enable the `POSTGIS` extension**: Server parameters > search `azure.extensions` > check `POSTGIS` > Save
- Note the server hostname, database name, username, and password for later

### 3. Storage Account

Create a storage account and add two blob containers:
- `preflightapi-airport-diagrams-<region>-<env>` (e.g., `preflightapi-airport-diagrams-centralus-test`)
- `preflightapi-chart-supplements-<region>-<env>` (e.g., `preflightapi-chart-supplements-centralus-test`)

### 4. Web App (API)

Create via Azure Portal:
- **Runtime stack**: .NET 8 (LTS), Linux
- **Plan**: B1 or higher (Linux App Service Plan)
- During creation, connect to GitHub repo and select the target branch (e.g., `develop`)
- The portal will auto-create an **App Registration** with OIDC federated credentials and push GitHub secrets to your repo

#### Web App Environment Variables

Set these under **Environment variables > App settings**:

| Setting | Value |
|---------|-------|
| `Database__Host` | `<postgres-server>.postgres.database.azure.com` |
| `Database__Database` | `<database-name>` |
| `Database__Username` | `<db-username>` |
| `Database__Password` | `<db-password>` |
| `Database__Port` | `5432` |
| `CloudStorage__UseManagedIdentity` | `true` |
| `CloudStorage__AccountName` | `<storage-account-name>` |
| `CloudStorage__ChartSupplementsContainerName` | `<chart-supplements-container>` |
| `CloudStorage__AirportDiagramsContainerName` | `<airport-diagrams-container>` |
| `NOAASettings__NOAAApiKey` | `<noaa-api-key>` |
| `NmsSettings__BaseUrl` | `https://api-staging.cgifederal-aim.com/nmsapi` |
| `NmsSettings__AuthBaseUrl` | `https://api-staging.cgifederal-aim.com` |
| `NmsSettings__ClientId` | `<nms-client-id>` |
| `NmsSettings__ClientSecret` | `<nms-client-secret>` |

Optional (production):
| Setting | Value |
|---------|-------|
| `KeyVault__Url` | `<key-vault-url>` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `<app-insights-connection-string>` |

### 5. Function App (Cron Jobs)

Create via Azure Portal:
- **Runtime stack**: .NET 8 Isolated
- **Plan**: Flex Consumption (or regular Consumption)
- During creation, connect to GitHub repo and select the target branch
- The portal will auto-create a separate **App Registration** with OIDC federated credentials

#### Function App Environment Variables

Set these under **Environment variables > App settings**:

| Setting | Value |
|---------|-------|
| `Database__Host` | `<postgres-server>.postgres.database.azure.com` |
| `Database__Database` | `<database-name>` |
| `Database__Username` | `<db-username>` |
| `Database__Password` | `<db-password>` |
| `Database__Port` | `5432` |
| `CloudStorage__UseManagedIdentity` | `true` |
| `CloudStorage__AccountName` | `<storage-account-name>` |
| `CloudStorage__ChartSupplementsContainerName` | `<chart-supplements-container>` |
| `CloudStorage__AirportDiagramsContainerName` | `<airport-diagrams-container>` |
| `AzureWebJobsStorage` | `<azure-storage-connection-string>` |

**Important (Flex Consumption)**: Do NOT add `FUNCTIONS_WORKER_RUNTIME` as an app setting. Flex Consumption manages the runtime at the platform level and rejects this setting. The runtime is configured during Function App creation.

The Function App does NOT need NOAA, NMS, or KeyVault settings — those are API-only.

### 6. Managed Identity & Role Assignments

#### Enable Managed Identity

For both the Web App and Function App:
- Go to **Settings > Identity > System assigned** > set to **On** > Save

#### Storage Access

Grant each managed identity **Storage Blob Data Contributor** role on the storage account:
- Storage account > **Access control (IAM)** > Add role assignment
- Role: **Storage Blob Data Contributor** (under Job function roles)
- Members: Select both the Web App and Function App identities

#### Database Firewall Access (for CI/CD migrations)

The CI/CD pipeline dynamically whitelists the GitHub runner's IP to run EF Core migrations. The Web App's OIDC App Registration needs permission to manage firewall rules:
- PostgreSQL server > **Access control (IAM)** > Add role assignment
- Role: **Contributor** (under Privileged administrator roles)
- Member: Search by the App Registration's object ID (visible in GitHub Actions error logs if misconfigured)

Note: This grants the App Registration Contributor on the database resource only, not the entire resource group.

## GitHub Secrets

The portal auto-creates OIDC secrets when you connect each app to GitHub. You need to manually add:

| Secret | Value | Purpose |
|--------|-------|---------|
| `DB_CONNECTION_STRING_TEST` | Full Npgsql connection string | EF Core migration bundle |

### Connection String Format

```
Host=<server>.postgres.database.azure.com;Database=<database-name>;Username=<user>;Password=<pass>;Port=5432;SSL Mode=Require;Trust Server Certificate=true
```

**Important**: Make sure the `Database=` parameter matches your actual database name, not the default `postgres` database.

### Portal-Created Secrets (auto-generated)

The portal creates these with auto-generated suffix names:
- `AZUREAPPSERVICE_CLIENTID_<hash>` — App Registration client ID
- `AZUREAPPSERVICE_TENANTID_<hash>` — Azure AD tenant ID
- `AZUREAPPSERVICE_SUBSCRIPTIONID_<hash>` — Azure subscription ID

One set is created per app (Web App and Function App). The CI/CD workflow references these by their exact names.

## CI/CD Workflow

Single consolidated workflow at `.github/workflows/develop-ci-cd.yml`:

1. **Build job**: Restore, build, test, publish API + Functions, create EF migration bundle
2. **Migrations job**: Azure login > whitelist runner IP on database firewall > apply migrations > remove firewall rule
3. **Deploy job**: Deploy API via `azure/webapps-deploy`, deploy Functions via `az functionapp deployment source config-zip`

### Key CI/CD Details

- **Database firewall**: Runner IP is dynamically whitelisted using `az postgres flexible-server firewall-rule create` and cleaned up with `if: always()` to ensure removal even on failure
- **Functions deployment**: Uses Azure CLI zip deployment (`az functionapp deployment source config-zip`) instead of `azure/functions-action@v1` because Flex Consumption has compatibility issues with the GitHub Action
- **OIDC authentication**: Uses two separate Azure logins in the deploy job — one for the Web App identity (also used for migrations) and one for the Function App identity
- **Path filtering**: Workflow only triggers when code in project directories changes

## Gotchas & Lessons Learned

1. **Flex Consumption + `FUNCTIONS_WORKER_RUNTIME`**: Flex Consumption rejects this app setting. The runtime is platform-managed. Use Azure CLI zip deployment instead of `azure/functions-action`.

2. **Database firewall**: GitHub Actions runners have random public IPs that are NOT in Azure's network. You must either allow all IPs (fine for test) or dynamically whitelist the runner IP (preferred).

3. **OIDC App Registration permissions**: Portal-created App Registrations are scoped to their specific app. To manage database firewall rules from CI/CD, you must explicitly grant Contributor on the PostgreSQL resource.

4. **EF migration bundle connection string**: The `Database=` parameter must be explicit. Omitting it causes Npgsql to connect to the default `postgres` database.

5. **Azure CLI deprecation (May 2026)**: `az postgres flexible-server firewall-rule` arguments are changing in CLI 2.86.0. `--name` will become the rule name and `--server-name` will replace it for the server. Update workflows after the breaking change.

## Production Checklist

When provisioning production, repeat the above steps with production resource names and additionally:
- [ ] Use production NMS API URLs (not staging)
- [ ] Configure Azure Key Vault for secrets (NMS credentials)
- [ ] Enable Application Insights
- [ ] Consider stricter database firewall rules
- [ ] Use production-tier App Service Plan (B2+ or Standard)
- [ ] Configure custom domain and SSL if needed
- [ ] Set `ASPNETCORE_ENVIRONMENT` to `Production`
- [ ] Review Function App timer schedules for production intervals
- [ ] Create a separate `main-ci-cd.yml` workflow for production deployments
