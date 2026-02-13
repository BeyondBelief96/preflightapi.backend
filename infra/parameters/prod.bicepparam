using '../main.bicep'

param location = 'eastus'
param environment = 'prod'

// PostgreSQL
param dbAdminLogin = 'preflightapi_admin'
param dbAdminPassword = readEnvironmentVariable('DB_ADMIN_PASSWORD')
param databaseName = 'preflightapi'
param dbSkuName = 'Standard_B1ms'
param dbSkuTier = 'Burstable'
param dbStorageSizeGB = 32
param dbLocation = 'eastus2'

// Storage
param storageAccountName = 'stpreflightapiprod'
param storageSkuName = 'Standard_LRS'

// App Service
param apiSkuName = 'B1'
param apiSkuTier = 'Basic'

// APIM
param apimPublisherEmail = readEnvironmentVariable('APIM_PUBLISHER_EMAIL')
param apimSkuName = 'Developer'

// Secrets (sourced from environment variables / pipeline secrets)
param noaaApiKey = readEnvironmentVariable('NOAA_API_KEY')
param nmsClientId = readEnvironmentVariable('NMS_CLIENT_ID')
param nmsClientSecret = readEnvironmentVariable('NMS_CLIENT_SECRET')
param gatewaySecret = readEnvironmentVariable('GATEWAY_SECRET')

// NMS API URLs (production)
param nmsBaseUrl = 'https://api-nms.aim.faa.gov/nmsapi'
param nmsAuthBaseUrl = 'https://api-nms.aim.faa.gov'

// GitHub OIDC SP (set after running setup-github-oidc.sh)
param githubDeploymentPrincipalId = readEnvironmentVariable('GITHUB_DEPLOYMENT_PRINCIPAL_ID', '')
