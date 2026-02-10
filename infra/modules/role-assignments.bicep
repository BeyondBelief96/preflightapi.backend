@description('Principal ID of the Web App managed identity')
param webAppPrincipalId string

@description('Principal ID of the Function App managed identity')
param functionAppPrincipalId string

@description('Resource ID of the storage account for blob data access')
param storageAccountId string

@description('Principal ID of the GitHub deployment identity (optional)')
param githubDeploymentPrincipalId string = ''

// Built-in role definition IDs
// https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'

// Storage Blob Data Contributor — Web App
resource webAppStorageRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, webAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Blob Data Contributor — Function App
resource functionAppStorageRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, functionAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Contributor on resource group — GitHub deployment identity
// Covers: Web App deploy, Function App deploy, PostgreSQL firewall rules, APIM policy updates
resource githubRgContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(githubDeploymentPrincipalId)) {
  name: guid(resourceGroup().id, githubDeploymentPrincipalId, contributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
    principalId: githubDeploymentPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Reference existing resources by ID for scoping
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: last(split(storageAccountId, '/'))
}
