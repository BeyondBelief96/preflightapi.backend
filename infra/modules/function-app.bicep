@description('Azure region for all resources')
param location string

@description('Base name prefix for resources')
param baseName string

@description('Environment tag (test, prod)')
param environment string

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

@description('Airport diagrams blob container name')
param airportDiagramsContainerName string

@description('Chart supplements blob container name')
param chartSupplementsContainerName string

var functionAppName = 'az-func-${baseName}-${environment}'
var functionsPlanName = 'asp-${baseName}-func-${environment}'
var functionsStorageName = 'st${replace(baseName, '-', '')}fn${environment}'

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
  name: functionsPlanName
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
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
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
          name: 'CloudStorage__AirportDiagramsContainerName'
          value: airportDiagramsContainerName
        }
        {
          name: 'CloudStorage__ChartSupplementsContainerName'
          value: chartSupplementsContainerName
        }
      ]
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
