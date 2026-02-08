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
param dbLocation = 'eastus2'

// Storage (existing manually-created test storage account)
param storageAccountName = 'rgpreflightapieastusad'

// App Service
param apiSkuName = 'B2'
param apiSkuTier = 'Basic'

// APIM
param apimPublisherEmail = readEnvironmentVariable('APIM_PUBLISHER_EMAIL')
param apimSkuName = 'Developer'

// Secrets (sourced from environment variables / pipeline secrets)
param noaaApiKey = readEnvironmentVariable('NOAA_API_KEY')
param nmsClientId = readEnvironmentVariable('NMS_CLIENT_ID')
param nmsClientSecret = readEnvironmentVariable('NMS_CLIENT_SECRET')
param gatewaySecret = readEnvironmentVariable('GATEWAY_SECRET')

// NMS API URLs (staging for test)
param nmsBaseUrl = 'https://api-staging.cgifederal-aim.com/nmsapi'
param nmsAuthBaseUrl = 'https://api-staging.cgifederal-aim.com'

// GitHub OIDC SP (set after running setup-github-oidc.sh)
param githubDeploymentPrincipalId = readEnvironmentVariable('GITHUB_DEPLOYMENT_PRINCIPAL_ID', '')
