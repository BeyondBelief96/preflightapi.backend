targetScope = 'subscription'

// ─── Environment ─────────────────────────────────────────────────────────────

@description('Primary Azure region for resources')
param location string

@description('Environment name (test, prod)')
@allowed(['test', 'prod'])
param environment string

// ─── Resource Names ──────────────────────────────────────────────────────────
// Fully parameterized — each environment specifies exact names matching what's
// already deployed. No names are derived, so there's no naming drift between
// Bicep and what exists in the portal.

@description('Resource group name')
param resourceGroupName string

@description('Log Analytics workspace name')
param logAnalyticsName string

@description('Application Insights resource name')
param appInsightsName string

@description('PostgreSQL flexible server name')
param postgresServerName string

@description('Azure region for PostgreSQL (can differ from primary location)')
param postgresLocation string = location

@description('Storage account name for blob data (globally unique, 3-24 lowercase alphanumeric)')
param storageAccountName string

@description('Terminal procedures blob container name')
param terminalProceduresContainerName string

@description('Chart supplements blob container name')
param chartSupplementsContainerName string

@description('App Service Plan name for the API')
param appServicePlanName string

@description('Web App name for the API')
param webAppName string

@description('Function App Flex Consumption plan name')
param functionsPlanName string

@description('Function App name')
param functionAppName string

@description('Storage account name for Functions runtime (AzureWebJobsStorage)')
param functionsStorageName string

@description('API Management service name')
param apimServiceName string

// ─── PostgreSQL ──────────────────────────────────────────────────────────────

@description('PostgreSQL administrator login')
param dbAdminLogin string

@secure()
@description('PostgreSQL administrator password')
param dbAdminPassword string

@description('PostgreSQL database name')
param databaseName string = 'preflightapi'

@description('PostgreSQL SKU name')
param dbSkuName string = 'Standard_B1ms'

@description('PostgreSQL SKU tier')
param dbSkuTier string = 'Burstable'

@description('PostgreSQL storage size in GB')
param dbStorageSizeGB int = 32

@description('PostgreSQL major version')
param dbVersion string = '16'

// ─── Storage ─────────────────────────────────────────────────────────────────

@description('Storage account SKU')
param storageSkuName string = 'Standard_LRS'

// ─── App Service ─────────────────────────────────────────────────────────────

@description('App Service Plan SKU name')
param apiSkuName string = 'B1'

@description('App Service Plan SKU tier')
param apiSkuTier string = 'Basic'

// ─── APIM ────────────────────────────────────────────────────────────────────

@description('APIM publisher email')
param apimPublisherEmail string

@description('APIM SKU name (Developer, BasicV2, StandardV2, etc.)')
param apimSkuName string = 'BasicV2'

@description('APIM SKU capacity')
param apimSkuCapacity int = 1

// ─── Secrets ─────────────────────────────────────────────────────────────────

@secure()
@description('NOAA API key for weather data synchronization (used by Functions)')
param noaaApiKey string

@secure()
@description('APIM-to-API shared secret for gateway validation')
param gatewaySecret string

// ─── NMS Settings (Azure Functions) ──────────────────────────────────────────

@description('NMS API base URL')
param nmsBaseUrl string

@description('NMS OAuth2 auth base URL')
param nmsAuthBaseUrl string

@secure()
@description('NMS OAuth2 client ID')
param nmsClientId string

@secure()
@description('NMS OAuth2 client secret')
param nmsClientSecret string

// ─── Clerk Settings (optional) ───────────────────────────────────────────────

@description('Clerk JWT authority URL (leave empty to omit from app settings)')
param clerkAuthority string = ''

// ─── GitHub Deployment Identity ──────────────────────────────────────────────
// Provide the Object ID of your GitHub deployment service principal (App
// Registration or Managed Identity). Used for Contributor RBAC on the resource
// group. Leave empty to skip the role assignment.

@description('Object ID of the GitHub deployment service principal')
param githubDeploymentPrincipalId string = ''

// ─── Resource Group ──────────────────────────────────────────────────────────

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

// ─── Modules ─────────────────────────────────────────────────────────────────

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-${environment}'
  scope: rg
  params: {
    location: location
    logAnalyticsName: logAnalyticsName
    appInsightsName: appInsightsName
  }
}

module postgresql 'modules/postgresql.bicep' = {
  name: 'postgresql-${environment}'
  scope: rg
  params: {
    location: postgresLocation
    serverName: postgresServerName
    administratorLogin: dbAdminLogin
    administratorPassword: dbAdminPassword
    databaseName: databaseName
    skuName: dbSkuName
    skuTier: dbSkuTier
    storageSizeGB: dbStorageSizeGB
    version: dbVersion
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage-${environment}'
  scope: rg
  params: {
    location: location
    storageAccountName: storageAccountName
    skuName: storageSkuName
    terminalProceduresContainerName: terminalProceduresContainerName
    chartSupplementsContainerName: chartSupplementsContainerName
  }
}

module appService 'modules/app-service.bicep' = {
  name: 'app-service-${environment}'
  scope: rg
  params: {
    location: location
    planName: appServicePlanName
    webAppName: webAppName
    skuName: apiSkuName
    skuTier: apiSkuTier
    environment: environment
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    databaseHost: postgresql.outputs.serverFqdn
    databaseName: databaseName
    databaseUsername: dbAdminLogin
    databasePassword: dbAdminPassword
    storageAccountName: storage.outputs.storageAccountName
    terminalProceduresContainerName: storage.outputs.terminalProceduresContainerName
    chartSupplementsContainerName: storage.outputs.chartSupplementsContainerName
    gatewaySecret: gatewaySecret
  }
}

module functionApp 'modules/function-app.bicep' = {
  name: 'function-app-${environment}'
  scope: rg
  params: {
    location: location
    planName: functionsPlanName
    functionAppName: functionAppName
    functionsStorageName: functionsStorageName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    databaseHost: postgresql.outputs.serverFqdn
    databaseName: databaseName
    databaseUsername: dbAdminLogin
    databasePassword: dbAdminPassword
    storageAccountName: storage.outputs.storageAccountName
    terminalProceduresContainerName: storage.outputs.terminalProceduresContainerName
    chartSupplementsContainerName: storage.outputs.chartSupplementsContainerName
    nmsBaseUrl: nmsBaseUrl
    nmsAuthBaseUrl: nmsAuthBaseUrl
    nmsClientId: nmsClientId
    nmsClientSecret: nmsClientSecret
    noaaApiKey: noaaApiKey
    gatewaySecret: gatewaySecret
    clerkAuthority: clerkAuthority
  }
}

module apim 'modules/apim.bicep' = {
  name: 'apim-${environment}'
  scope: rg
  params: {
    location: location
    apimName: apimServiceName
    publisherEmail: apimPublisherEmail
    skuName: apimSkuName
    skuCapacity: apimSkuCapacity
    backendWebAppHostName: appService.outputs.webAppHostName
    gatewaySecret: gatewaySecret
  }
}

module roleAssignments 'modules/role-assignments.bicep' = {
  name: 'role-assignments-${environment}'
  scope: rg
  params: {
    webAppPrincipalId: appService.outputs.webAppPrincipalId
    functionAppPrincipalId: functionApp.outputs.functionAppPrincipalId
    storageAccountId: storage.outputs.storageAccountId
    githubDeploymentPrincipalId: githubDeploymentPrincipalId
  }
}

// ─── Outputs ─────────────────────────────────────────────────────────────────

@description('Resource group name')
output resourceGroupName string = rg.name

@description('PostgreSQL server FQDN')
output postgresServerFqdn string = postgresql.outputs.serverFqdn

@description('PostgreSQL server name')
output postgresServerName string = postgresql.outputs.serverName

@description('Web App name')
output webAppName string = appService.outputs.webAppName

@description('Web App hostname')
output webAppHostName string = appService.outputs.webAppHostName

@description('Function App name')
output functionAppName string = functionApp.outputs.functionAppName

@description('Function App hostname')
output functionAppHostName string = functionApp.outputs.functionAppHostName

@description('Storage account name')
output storageAccountName string = storage.outputs.storageAccountName

@description('APIM gateway URL')
output apimGatewayUrl string = apim.outputs.apimGatewayUrl

@description('APIM service name')
output apimServiceName string = apim.outputs.apimName

@description('Application Insights connection string')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString
