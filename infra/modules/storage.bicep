@description('Azure region for all resources')
param location string

@description('Storage account name (must be globally unique, 3-24 lowercase alphanumeric)')
param storageAccountName string

@description('Storage account SKU')
param skuName string = 'Standard_LRS'

@description('Terminal procedures blob container name')
param terminalProceduresContainerName string

@description('Chart supplements blob container name')
param chartSupplementsContainerName string

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

// Terminal Procedures container
resource terminalProceduresContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: terminalProceduresContainerName
  properties: {
    publicAccess: 'None'
  }
}

// Chart Supplements container
resource chartSupplementsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: chartSupplementsContainerName
  properties: {
    publicAccess: 'None'
  }
}

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Storage account resource ID')
output storageAccountId string = storageAccount.id

@description('Terminal procedures container name')
output terminalProceduresContainerName string = terminalProceduresContainer.name

@description('Chart supplements container name')
output chartSupplementsContainerName string = chartSupplementsContainer.name
