@description('Azure region for all resources')
param location string

@description('Storage account name for blob data (globally unique, 3-24 lowercase alphanumeric)')
param storageAccountName string

@description('Storage account SKU')
param skuName string = 'Standard_LRS'

@description('Terminal procedures blob container name')
param terminalProceduresContainerName string

@description('Chart supplements blob container name')
param chartSupplementsContainerName string

@description('PreflightApi resources blob container name')
param preflightApiResourcesContainerName string

@description('Separate storage account name for Functions runtime (leave empty to share the data account)')
param functionsStorageName string = ''

// ─── Data Storage Account ───────────────────────────────────────────────────

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

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource terminalProceduresContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: terminalProceduresContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource chartSupplementsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: chartSupplementsContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource preflightApiResourcesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: preflightApiResourcesContainerName
  properties: {
    publicAccess: 'None'
  }
}

// ─── Dedicated Functions Storage Account (optional) ─────────────────────────
// When functionsStorageName is non-empty and differs from the data account,
// create a second storage account for Azure Functions runtime (AzureWebJobsStorage).

var createFunctionsStorage = !empty(functionsStorageName) && functionsStorageName != storageAccountName

resource functionsStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = if (createFunctionsStorage) {
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

// ─── Outputs ────────────────────────────────────────────────────────────────

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Storage account resource ID')
output storageAccountId string = storageAccount.id

@description('Terminal procedures container name')
output terminalProceduresContainerName string = terminalProceduresContainer.name

@description('Chart supplements container name')
output chartSupplementsContainerName string = chartSupplementsContainer.name

@description('PreflightApi resources container name')
output preflightApiResourcesContainerName string = preflightApiResourcesContainer.name

@description('Functions storage account name (shared or dedicated)')
output functionsStorageAccountName string = createFunctionsStorage ? functionsStorageName : storageAccountName
