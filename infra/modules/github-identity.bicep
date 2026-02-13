// ─── Standalone Module ────────────────────────────────────────────────────────
// Creates a user-assigned managed identity with GitHub OIDC federated credentials.
// This module is NOT wired into main.bicep by default — both PRD and TST use
// existing App Registrations (service principals) for GitHub Actions OIDC.
//
// Use this module standalone for new environments:
//   az deployment group create \
//     --resource-group <rg-name> \
//     --template-file modules/github-identity.bicep \
//     --parameters identityName=id-preflightapi-github-prod ...
//
// Then set the output principalId as GITHUB_DEPLOYMENT_PRINCIPAL_ID in .env.

@description('Azure region for the managed identity')
param location string

@description('Name for the managed identity resource')
param identityName string

@description('GitHub organization or username')
param githubOrganization string = 'BeyondBelief96'

@description('GitHub repository name')
param githubRepository string = 'preflightapi.backend'

@description('Branches to create federated credentials for')
param branches array = [
  'main'
  'develop'
]

// ─── User-Assigned Managed Identity ──────────────────────────────────────────

resource githubIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
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
