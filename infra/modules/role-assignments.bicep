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

// ─── Built-in Role Definition IDs ───────────────────────────────────────────
// https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles

var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var keyVaultCertificatesOfficerRoleId = 'a4417e6f-fecd-4de8-b567-7b0420556985'

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

// Web App → Key Vault Secrets User (for pulling custom-domain cert from Key Vault)
resource webAppKeyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webAppPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ─── Key Vault Certificates Officer ──────────────────────────────────────────

// Function App → Key Vault Certificates Officer (for certificate renewal: get + import)
resource functionAppKeyVaultCertRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, functionAppPrincipalId, keyVaultCertificatesOfficerRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultCertificatesOfficerRoleId)
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
