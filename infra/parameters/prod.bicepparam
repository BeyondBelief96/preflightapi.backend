using '../main.bicep'

param location = 'eastus'
param environment = 'prod'

// ─── Resource Names (matching actual PRD infrastructure) ─────────────────────

param resourceGroupName = 'rg-eastus-preflightapi-prd'
param logAnalyticsName = 'log-eastus-preflightapi-prd'
param appInsightsName = 'was-eastus-preflightapi-prd' // Portal named AI same as web app
param postgresServerName = 'pgsql-eastus2-preflightapi-prd'
param postgresLocation = 'eastus2'
param storageAccountName = 'stpreflightapiprod'
param terminalProceduresContainerName = 'preflightapi-terminal-procedures-eastus-prod'
param chartSupplementsContainerName = 'preflightapi-chart-supplements-eastus-prod'
param appServicePlanName = 'asp-eastus-preflightapi-api-prd'
param webAppName = 'was-eastus-preflightapi-prd'
param functionsPlanName = 'asp-eastus-preflightapi-func-prd'
param functionAppName = 'function-app-eastus-preflightapi-prd'
param functionsStorageName = 'stpreflightapifnprd'
param apimServiceName = 'apim-eastus-preflightapi-prd'

// ─── PostgreSQL ──────────────────────────────────────────────────────────────

param dbAdminLogin = 'preflightapi_admin'
param dbAdminPassword = readEnvironmentVariable('DB_ADMIN_PASSWORD')
param databaseName = 'preflightapi'
param dbSkuName = 'Standard_B1ms'
param dbSkuTier = 'Burstable'
param dbStorageSizeGB = 32
param dbVersion = '16'

// ─── Storage ─────────────────────────────────────────────────────────────────

param storageSkuName = 'Standard_LRS'

// ─── App Service ─────────────────────────────────────────────────────────────

param apiSkuName = 'B1'
param apiSkuTier = 'Basic'

// ─── APIM ────────────────────────────────────────────────────────────────────

param apimPublisherEmail = readEnvironmentVariable('APIM_PUBLISHER_EMAIL')
param apimSkuName = 'BasicV2'
param apimSkuCapacity = 1

// ─── Secrets ─────────────────────────────────────────────────────────────────

param noaaApiKey = readEnvironmentVariable('NOAA_API_KEY')
param gatewaySecret = readEnvironmentVariable('GATEWAY_SECRET')

// ─── NMS Settings (production endpoints) ─────────────────────────────────────

param nmsBaseUrl = 'https://api-nms.aim.faa.gov/nmsapi'
param nmsAuthBaseUrl = 'https://api-nms.aim.faa.gov'
param nmsClientId = readEnvironmentVariable('NMS_CLIENT_ID')
param nmsClientSecret = readEnvironmentVariable('NMS_CLIENT_SECRET')

// ─── Clerk Settings ──────────────────────────────────────────────────────────

param clerkAuthority = readEnvironmentVariable('CLERK_AUTHORITY', '')

// ─── GitHub Deployment Identity ──────────────────────────────────────────────
// Object ID of the existing App Registration service principal used for
// GitHub Actions OIDC. Provides Contributor RBAC on the resource group.

param githubDeploymentPrincipalId = readEnvironmentVariable('GITHUB_DEPLOYMENT_PRINCIPAL_ID', '')
