using '../main.bicep'

param location = 'eastus'
param environment = 'test'

// ─── Resource Names (matching actual TST infrastructure) ─────────────────────

param resourceGroupName = 'rg-preflightapi-eastus-test'
param logAnalyticsName = 'log-preflightapi-eastus-test'
param appInsightsName = 'appi-preflightapi-eastus-test'
param postgresServerName = 'pgsql-preflightapi-eastus-test'
param postgresLocation = 'eastus2'
param storageAccountName = 'rgpreflightapieastusad'
param airportDiagramsContainerName = 'preflightapi-airport-diagrams-eastus-test'
param chartSupplementsContainerName = 'preflightapi-chart-supplements-eastus-test'
param appServicePlanName = 'asp-preflightapi-eastus-api-test'
param webAppName = 'preflightapi-eastus-web-api-test'
param functionsPlanName = 'asp-preflightapi-eastus-func-test'
param functionAppName = 'az-func-preflightapi-eastus-test'
param functionsStorageName = 'stpreflightapifntest'
param apimServiceName = 'preflightapi-apim-service-test'

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
param apimSkuName = 'Developer'
param apimSkuCapacity = 1

// ─── Secrets ─────────────────────────────────────────────────────────────────

param noaaApiKey = readEnvironmentVariable('NOAA_API_KEY')
param gatewaySecret = readEnvironmentVariable('GATEWAY_SECRET')

// ─── NMS Settings (staging endpoints) ────────────────────────────────────────

param nmsBaseUrl = 'https://api-staging.cgifederal-aim.com/nmsapi'
param nmsAuthBaseUrl = 'https://api-staging.cgifederal-aim.com'
param nmsClientId = readEnvironmentVariable('NMS_CLIENT_ID')
param nmsClientSecret = readEnvironmentVariable('NMS_CLIENT_SECRET')

// ─── Clerk Settings ──────────────────────────────────────────────────────────

param clerkAuthority = readEnvironmentVariable('CLERK_AUTHORITY', '')

// ─── GitHub Deployment Identity ──────────────────────────────────────────────

param githubDeploymentPrincipalId = readEnvironmentVariable('GITHUB_DEPLOYMENT_PRINCIPAL_ID', '')
