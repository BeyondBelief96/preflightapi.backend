@description('Storage account name for blob data')
param storageAccountName string

@description('Principal ID of the Web App managed identity')
param webAppPrincipalId string

@description('Principal ID of the Function App managed identity')
param functionAppPrincipalId string

@description('Principal ID of the GitHub deployment identity (optional)')
param githubDeploymentPrincipalId string = ''

@description('Key Vault name for RBAC assignments')
param keyVaultName string

@description('PostgreSQL server name for RBAC assignments')
param postgresServerName string

@description('APIM service name for RBAC assignments')
param apimName string

@description('Log Analytics workspace name for RBAC assignments')
param logAnalyticsWorkspaceName string

@description('APIM service principal ID for frontend management (optional)')
param apimServicePrincipalId string = ''

// ─── Built-in Role Definition IDs ───────────────────────────────────────────
// https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles

var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var apiManagementServiceContributorRoleId = '312a565d-c81f-4fd8-895a-4e21e48d571c'
var logAnalyticsReaderRoleId = '73c42c96-874c-492b-b04d-ab87d138a893'

// ─── Existing Resource References ───────────────────────────────────────────

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
  name: postgresServerName
}

resource apim 'Microsoft.ApiManagement/service@2024-05-01' existing = {
  name: apimName
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

// ─── Storage Blob Data Contributor ──────────────────────────────────────────

// Web App → Storage Blob Data Contributor
resource webAppStorageRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, webAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Function App → Storage Blob Data Contributor
resource functionAppStorageRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ─── Key Vault Secrets User ─────────────────────────────────────────────────

// Function App → Key Vault Secrets User
resource functionAppKeyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, functionAppPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: functionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ─── GitHub Deployment SP ───────────────────────────────────────────────────

// Contributor on resource group
resource githubRgContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(githubDeploymentPrincipalId)) {
  name: guid(resourceGroup().id, githubDeploymentPrincipalId, contributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
    principalId: githubDeploymentPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Contributor on PostgreSQL server (for CI/CD firewall rules)
resource githubPostgresContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(githubDeploymentPrincipalId)) {
  name: guid(postgresServer.id, githubDeploymentPrincipalId, contributorRoleId)
  scope: postgresServer
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
    principalId: githubDeploymentPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// API Management Service Contributor on APIM (for CI/CD policy deployment)
resource githubApimContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(githubDeploymentPrincipalId)) {
  name: guid(apim.id, githubDeploymentPrincipalId, apiManagementServiceContributorRoleId)
  scope: apim
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', apiManagementServiceContributorRoleId)
    principalId: githubDeploymentPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ─── APIM Service Principal (frontend management) ──────────────────────────

// API Management Service Contributor on APIM
resource apimSpContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apimServicePrincipalId)) {
  name: guid(apim.id, apimServicePrincipalId, apiManagementServiceContributorRoleId)
  scope: apim
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', apiManagementServiceContributorRoleId)
    principalId: apimServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Log Analytics Reader on Log Analytics workspace
resource apimSpLogAnalyticsReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apimServicePrincipalId)) {
  name: guid(logAnalyticsWorkspace.id, apimServicePrincipalId, logAnalyticsReaderRoleId)
  scope: logAnalyticsWorkspace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', logAnalyticsReaderRoleId)
    principalId: apimServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}
