targetScope = 'subscription'

// ─── Parameters ────────────────────────────────────────────────────────────────

@description('Azure region for all resources')
param location string

@description('Environment name (test, prod)')
@allowed(['test', 'prod'])
param environment string

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

@description('Azure region for the PostgreSQL server (defaults to location if not specified)')
param dbLocation string = location

@description('Storage account name for blob data (must be globally unique)')
param storageAccountName string

@description('Storage account SKU')
param storageSkuName string = 'Standard_LRS'

@description('App Service Plan SKU name for API')
param apiSkuName string = 'B1'

@description('App Service Plan SKU tier for API')
param apiSkuTier string = 'Basic'

@description('APIM publisher email')
param apimPublisherEmail string

@description('APIM SKU (Developer, Basic, Standard, Premium)')
param apimSkuName string = 'Developer'

@secure()
@description('NOAA API key for weather services')
param noaaApiKey string

@description('NMS API base URL (staging or production)')
param nmsBaseUrl string

@description('NMS OAuth2 auth base URL (staging or production)')
param nmsAuthBaseUrl string

@secure()
@description('NMS OAuth2 client ID')
param nmsClientId string

@secure()
@description('NMS OAuth2 client secret')
param nmsClientSecret string

@secure()
@description('APIM-to-API shared secret for gateway validation')
param gatewaySecret string

@description('GitHub organization or username (for OIDC federated credentials)')
param githubOrganization string = 'BeyondBelief96'

@description('GitHub repository name (for OIDC federated credentials)')
param githubRepository string = 'preflightapi.backend'

@description('Branches to create federated credentials for')
param githubBranches array = [
  'main'
  'develop'
]

// ─── Derived values ────────────────────────────────────────────────────────────

var baseName = 'preflightapi-${location}'
var resourceGroupName = 'rg-${baseName}-${environment}'
var regionShortName = location

// ─── Resource Group ────────────────────────────────────────────────────────────

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

// ─── Modules ───────────────────────────────────────────────────────────────────

// Monitoring (deployed first — other modules depend on App Insights)
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-${environment}'
  scope: rg
  params: {
    location: location
    baseName: baseName
    environment: environment
  }
}

// PostgreSQL
module postgresql 'modules/postgresql.bicep' = {
  name: 'postgresql-${environment}'
  scope: rg
  params: {
    location: dbLocation
    baseName: baseName
    environment: environment
    administratorLogin: dbAdminLogin
    administratorPassword: dbAdminPassword
    skuName: dbSkuName
    skuTier: dbSkuTier
    storageSizeGB: dbStorageSizeGB
  }
}

// Storage Account + Blob Containers
module storage 'modules/storage.bicep' = {
  name: 'storage-${environment}'
  scope: rg
  params: {
    location: location
    regionShortName: regionShortName
    environment: environment
    storageAccountName: storageAccountName
    skuName: storageSkuName
  }
}

// App Service (API)
module appService 'modules/app-service.bicep' = {
  name: 'app-service-${environment}'
  scope: rg
  params: {
    location: location
    baseName: baseName
    environment: environment
    skuName: apiSkuName
    skuTier: apiSkuTier
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    databaseHost: postgresql.outputs.serverFqdn
    databaseName: databaseName
    databaseUsername: dbAdminLogin
    databasePassword: dbAdminPassword
    storageAccountName: storage.outputs.storageAccountName
    airportDiagramsContainerName: storage.outputs.airportDiagramsContainerName
    chartSupplementsContainerName: storage.outputs.chartSupplementsContainerName
    noaaApiKey: noaaApiKey
    nmsBaseUrl: nmsBaseUrl
    nmsAuthBaseUrl: nmsAuthBaseUrl
    nmsClientId: nmsClientId
    nmsClientSecret: nmsClientSecret
    gatewaySecret: gatewaySecret
  }
}

// Function App
module functionApp 'modules/function-app.bicep' = {
  name: 'function-app-${environment}'
  scope: rg
  params: {
    location: location
    baseName: baseName
    environment: environment
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    databaseHost: postgresql.outputs.serverFqdn
    databaseName: databaseName
    databaseUsername: dbAdminLogin
    databasePassword: dbAdminPassword
    storageAccountName: storage.outputs.storageAccountName
    airportDiagramsContainerName: storage.outputs.airportDiagramsContainerName
    chartSupplementsContainerName: storage.outputs.chartSupplementsContainerName
  }
}

// API Management
module apim 'modules/apim.bicep' = {
  name: 'apim-${environment}'
  scope: rg
  params: {
    location: location
    baseName: baseName
    environment: environment
    publisherEmail: apimPublisherEmail
    skuName: apimSkuName
    backendWebAppHostName: appService.outputs.webAppHostName
    gatewaySecret: gatewaySecret
  }
}

// GitHub Deployment Identity (OIDC federated credentials)
module githubIdentity 'modules/github-identity.bicep' = {
  name: 'github-identity-${environment}'
  scope: rg
  params: {
    location: location
    baseName: baseName
    environment: environment
    githubOrganization: githubOrganization
    githubRepository: githubRepository
    branches: githubBranches
  }
}

// Role Assignments (RBAC)
module roleAssignments 'modules/role-assignments.bicep' = {
  name: 'role-assignments-${environment}'
  scope: rg
  params: {
    webAppPrincipalId: appService.outputs.webAppPrincipalId
    functionAppPrincipalId: functionApp.outputs.functionAppPrincipalId
    storageAccountId: storage.outputs.storageAccountId
    githubDeploymentPrincipalId: githubIdentity.outputs.principalId
  }
}

// ─── Outputs ───────────────────────────────────────────────────────────────────

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

@description('GitHub deployment identity client ID — set as AZURE_CLIENT_ID secret in GitHub')
output githubDeploymentClientId string = githubIdentity.outputs.clientId
