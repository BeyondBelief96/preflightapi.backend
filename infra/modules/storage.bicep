@description('Azure region for all resources')
param location string

@description('Region short name for container naming (e.g., eastus)')
param regionShortName string

@description('Environment tag (test, prod)')
param environment string

@description('Storage account name (must be globally unique, 3-24 lowercase alphanumeric)')
param storageAccountName string

@description('Storage account SKU')
param skuName string = 'Standard_RAGRS'

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: skuName
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// Blob Services
resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// Airport Diagrams container
resource airportDiagramsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'preflightapi-airport-diagrams-${regionShortName}-${environment}'
  properties: {
    publicAccess: 'None'
  }
}

// Chart Supplements container
resource chartSupplementsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'preflightapi-chart-supplements-${regionShortName}-${environment}'
  properties: {
    publicAccess: 'None'
  }
}

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Storage account resource ID')
output storageAccountId string = storageAccount.id

@description('Airport diagrams container name')
output airportDiagramsContainerName string = airportDiagramsContainer.name

@description('Chart supplements container name')
output chartSupplementsContainerName string = chartSupplementsContainer.name
