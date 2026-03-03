using '../main.bicep'

param location = 'eastus'
param environment = 'test'

// ─── Resource Names (sandbox — all unique names) ────────────────────────────

param resourceGroupName = 'rg-preflightapi-eastus-sandbox'
param logAnalyticsName = 'log-preflightapi-sandbox'
param apiAppInsightsName = 'appi-preflightapi-api-sandbox'
param functionAppInsightsName = 'appi-preflightapi-func-sandbox'
param postgresServerName = 'pgsql-preflightapi-sandbox'
param postgresLocation = 'eastus2'
param storageAccountName = 'stpreflightapisandbox'
param terminalProceduresContainerName = 'terminal-procedures'
param chartSupplementsContainerName = 'chart-supplements'
param preflightApiResourcesContainerName = 'preflightapi-resources'
param appServicePlanName = 'asp-preflightapi-api-sandbox'
param webAppName = 'preflightapi-web-sandbox'
param webAppPlatform = 'windows'
param functionsPlanName = 'asp-preflightapi-func-sandbox'
param functionAppName = 'preflightapi-func-sandbox'
param functionsStorageName = '' // Shared storage account (same as TST)
param keyVaultName = 'kv-preflightapi-sandbox'
param apimServiceName = 'apim-preflightapi-sandbox'

// ─── PostgreSQL ──────────────────────────────────────────────────────────────

param dbAdminLogin = 'preflightapi_admin'
param dbAdminPassword = readEnvironmentVariable('DB_ADMIN_PASSWORD')
param databaseName = 'preflightapi-sandbox-database'
param dbSkuName = 'Standard_B1ms'
param dbSkuTier = 'Burstable'
param dbStorageSizeGB = 32
param dbVersion = '15'

// ─── Storage ─────────────────────────────────────────────────────────────────

param storageSkuName = 'Standard_RAGRS'

// ─── App Service ─────────────────────────────────────────────────────────────

param apiSkuName = 'F1'
param apiSkuTier = 'Free'

// ─── APIM ────────────────────────────────────────────────────────────────────

param apimPublisherEmail = readEnvironmentVariable('APIM_PUBLISHER_EMAIL')
param apimSkuName = 'Developer'
param apimSkuCapacity = 1
param apimCustomDomainHostName = ''
param apimKeyVaultCertificateName = ''

// ─── Secrets ─────────────────────────────────────────────────────────────────

param noaaApiKey = readEnvironmentVariable('NOAA_API_KEY')
param gatewaySecret = readEnvironmentVariable('GATEWAY_SECRET')

// ─── NMS Settings (staging endpoints) ────────────────────────────────────────

param nmsBaseUrl = 'https://api-staging.cgifederal-aim.com/nmsapi'
param nmsAuthBaseUrl = 'https://api-staging.cgifederal-aim.com'
param nmsClientId = readEnvironmentVariable('NMS_CLIENT_ID')
param nmsClientSecret = readEnvironmentVariable('NMS_CLIENT_SECRET')

// ─── Clerk Settings ──────────────────────────────────────────────────────────

param clerkAuthority = readEnvironmentVariable('CLERK_AUTHORITY', '')
param clerkSecretKey = readEnvironmentVariable('CLERK_SECRET_KEY', '')

// ─── Certificate Renewal ────────────────────────────────────────────────────

param certificateAcmeEmail = readEnvironmentVariable('CERTIFICATE_ACME_EMAIL', '')
param certificateCertName = readEnvironmentVariable('CERTIFICATE_CERT_NAME', '')
param certificateDomain = readEnvironmentVariable('CERTIFICATE_DOMAIN', '')
param certificateRootDomain = readEnvironmentVariable('CERTIFICATE_ROOT_DOMAIN', '')

// ─── Porkbun DNS ────────────────────────────────────────────────────────────

param porkbunApiKey = readEnvironmentVariable('PORKBUN_API_KEY', '')
param porkbunSecretApiKey = readEnvironmentVariable('PORKBUN_SECRET_API_KEY', '')

// ─── Resend Email ───────────────────────────────────────────────────────────

param resendApiToken = readEnvironmentVariable('RESEND_API_TOKEN', '')
param resendEnabled = 'false'
param resendSegmentAllId = readEnvironmentVariable('RESEND_SEGMENT_ALL_ID', '')
param resendTopicAlertsId = readEnvironmentVariable('RESEND_TOPIC_ALERTS_ID', '')

// ─── GitHub Deployment Identity ──────────────────────────────────────────────

param githubDeploymentPrincipalId = readEnvironmentVariable('GITHUB_DEPLOYMENT_PRINCIPAL_ID', '')

// ─── APIM Service Principal (optional) ───────────────────────────────────────

param apimServicePrincipalId = readEnvironmentVariable('APIM_SERVICE_PRINCIPAL_ID', '')
