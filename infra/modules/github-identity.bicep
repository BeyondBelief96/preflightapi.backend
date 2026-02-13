@description('Azure region for the managed identity')
param location string

@description('Base name for resource naming')
param baseName string

@description('Environment name (test, prod)')
param environment string

@description('GitHub organization or username')
param githubOrganization string

@description('GitHub repository name')
param githubRepository string

@description('Branches to create federated credentials for')
param branches array = [
  'main'
  'develop'
]

// ─── User-Assigned Managed Identity ──────────────────────────────────────────

resource githubIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${baseName}-github-${environment}'
  location: location
}

// ─── Federated Identity Credentials (one per branch) ─────────────────────────

resource federatedCredentials 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = [
  for branch in branches: {
    parent: githubIdentity
    name: 'github-${replace(branch, '/', '-')}'
    properties: {
      issuer: 'https://token.actions.githubusercontent.com'
      subject: 'repo:${githubOrganization}/${githubRepository}:ref:refs/heads/${branch}'
      audiences: [
        'api://AzureADTokenExchange'
      ]
    }
  }
]

// ─── Outputs ─────────────────────────────────────────────────────────────────

@description('Principal (object) ID of the managed identity — use for RBAC role assignments')
output principalId string = githubIdentity.properties.principalId

@description('Client ID of the managed identity — use as AZURE_CLIENT_ID in GitHub Actions')
output clientId string = githubIdentity.properties.clientId
