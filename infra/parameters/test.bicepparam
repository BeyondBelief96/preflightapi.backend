using '../main.bicep'

param location = 'eastus'
param environment = 'test'

// PostgreSQL
param dbAdminLogin = 'preflightapi_admin'
param dbAdminPassword = readEnvironmentVariable('DB_ADMIN_PASSWORD')
param databaseName = 'preflightapi'
param dbSkuName = 'Standard_B1ms'
param dbSkuTier = 'Burstable'
param dbStorageSizeGB = 32

// Storage (must match existing test storage account name)
param storageAccountName = 'stpreflightapitest'

// App Service
param apiSkuName = 'B2'
param apiSkuTier = 'Basic'

// APIM
param apimPublisherEmail = readEnvironmentVariable('APIM_PUBLISHER_EMAIL')
param apimSkuName = 'Developer'

// GitHub OIDC SP (set after running setup-github-oidc.sh)
param githubDeploymentPrincipalId = readEnvironmentVariable('GITHUB_DEPLOYMENT_PRINCIPAL_ID', '')
