@description('Azure region for all resources')
param location string

@description('Function App Flex Consumption plan name')
param planName string

@description('Function App name')
param functionAppName string

@description('Storage account name for Functions runtime (AzureWebJobsStorage)')
param functionsStorageName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('PostgreSQL server FQDN')
param databaseHost string

@description('PostgreSQL database name')
param databaseName string

@description('PostgreSQL username')
param databaseUsername string

@secure()
@description('PostgreSQL password')
param databasePassword string

@description('Storage account name for managed identity access')
param storageAccountName string

@description('Terminal procedures blob container name')
param terminalProceduresContainerName string

@description('Chart supplements blob container name')
param chartSupplementsContainerName string

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

@secure()
@description('NOAA API key for weather data synchronization')
param noaaApiKey string

@secure()
@description('APIM-to-API shared secret for gateway validation')
param gatewaySecret string

@description('Clerk JWT authority URL (leave empty to omit)')
param clerkAuthority string = ''

// Storage Account for Azure Functions (AzureWebJobsStorage)
resource functionsStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: functionsStorageName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// Flex Consumption Plan
resource functionsPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'functionapp'
  sku: {
    tier: 'FlexConsumption'
    name: 'FC1'
  }
  properties: {
    reserved: true // Required for Linux
  }
}

// Build app settings — conditionally include Clerk settings
var baseAppSettings = [
  {
    name: 'AzureWebJobsStorage'
    value: 'DefaultEndpointsProtocol=https;AccountName=${functionsStorage.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${functionsStorage.listKeys().keys[0].value}'
  }
  {
    name: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
    value: 'DefaultEndpointsProtocol=https;AccountName=${functionsStorage.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${functionsStorage.listKeys().keys[0].value}'
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
  {
    name: 'Database__Host'
    value: databaseHost
  }
  {
    name: 'Database__Database'
    value: databaseName
  }
  {
    name: 'Database__Username'
    value: databaseUsername
  }
  {
    name: 'Database__Password'
    value: databasePassword
  }
  {
    name: 'Database__Port'
    value: '5432'
  }
  {
    name: 'CloudStorage__UseManagedIdentity'
    value: 'true'
  }
  {
    name: 'CloudStorage__AccountName'
    value: storageAccountName
  }
  {
    name: 'CloudStorage__TerminalProceduresContainerName'
    value: terminalProceduresContainerName
  }
  {
    name: 'CloudStorage__ChartSupplementsContainerName'
    value: chartSupplementsContainerName
  }
  {
    name: 'NmsSettings__BaseUrl'
    value: nmsBaseUrl
  }
  {
    name: 'NmsSettings__AuthBaseUrl'
    value: nmsAuthBaseUrl
  }
  {
    name: 'NmsSettings__ClientId'
    value: nmsClientId
  }
  {
    name: 'NmsSettings__ClientSecret'
    value: nmsClientSecret
  }
  {
    name: 'NOAASettings__NOAAApiKey'
    value: noaaApiKey
  }
  {
    name: 'GatewaySecret'
    value: gatewaySecret
  }
]

var clerkSettings = empty(clerkAuthority) ? [] : [
  {
    name: 'ClerkSettings__Authority'
    value: clerkAuthority
  }
  {
    name: 'ClerkSettings__RequireAuthenticationInDevelopment'
    value: 'true'
  }
]

// Function App
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionsPlan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: concat(baseAppSettings, clerkSettings)
    }
  }
}

@description('Function App name')
output functionAppName string = functionApp.name

@description('Function App default hostname')
output functionAppHostName string = functionApp.properties.defaultHostName

@description('Function App system-assigned managed identity principal ID')
output functionAppPrincipalId string = functionApp.identity.principalId

@description('Function App resource ID')
output functionAppId string = functionApp.id

@description('Functions storage account name')
output functionsStorageAccountName string = functionsStorage.name
