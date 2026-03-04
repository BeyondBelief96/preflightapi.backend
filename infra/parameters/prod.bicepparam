using '../main.bicep'

param location = 'eastus'
param environment = 'prod'

// ─── Resource Names (matching actual PRD infrastructure) ─────────────────────

param resourceGroupName = 'rg-eastus-preflightapi-prd'
param logAnalyticsName = 'preflightapi-log-analytics-workspace-apim-eastus-prd'
param apiAppInsightsName = 'was-eastus-preflightapi-prd'
param functionAppInsightsName = 'function-app-eastus-preflightapi-prd'
param postgresServerName = 'pgsql-eastus2-preflightapi-prd'
param postgresLocation = 'eastus2'
param storageAccountName = 'saeastuspreflightapiprd'
param terminalProceduresContainerName = 'sa-eastus-container-preflightapi-terminalprocedures-prd'
param chartSupplementsContainerName = 'sa-eastus-container-preflightapi-chartsupplements-prd'
param preflightApiResourcesContainerName = 'sa-eastus-container-preflightapi-resources-prd'
param appServicePlanName = 'ASP-rgeastuspreflightapiprd-95a4'
param webAppName = 'was-eastus-preflightapi-prd'
param webAppPlatform = 'linux'
param functionsPlanName = 'ASP-rgeastuspreflightapiprd-82d5'
param functionAppName = 'function-app-eastus-preflightapi-prd'
param functionsStorageName = 'rgeastuspreflightapb08e' // PRD uses a separate storage account for functions
param keyVaultName = 'KeyVaultPreflightApiPrd'
param apimServiceName = 'apim-eastus-preflightapi-prd'

// ─── PostgreSQL ──────────────────────────────────────────────────────────────

param dbAdminLogin = 'preflightapi_admin'
param dbAdminPassword = readEnvironmentVariable('DB_ADMIN_PASSWORD')
param databaseName = 'preflightapi'
param dbSkuName = 'Standard_B1ms'
param dbSkuTier = 'Burstable'
param dbStorageSizeGB = 32
param dbVersion = '16'

// ─── Storage ─────────────────────────────────────────────────────────────────

param storageSkuName = 'Standard_RAGRS'

// ─── App Service ─────────────────────────────────────────────────────────────

param apiSkuName = 'B2'
param apiSkuTier = 'Basic'

// ─── APIM ────────────────────────────────────────────────────────────────────

param apimPublisherEmail = readEnvironmentVariable('APIM_PUBLISHER_EMAIL')
param apimSkuName = 'BasicV2'
param apimSkuCapacity = 1
param apimCustomDomainHostName = 'api.preflightapi.io'
param apimKeyVaultCertificateName = 'api-preflightapi-io'

// ─── Secrets ─────────────────────────────────────────────────────────────────

param noaaApiKey = readEnvironmentVariable('NOAA_API_KEY')
param gatewaySecret = readEnvironmentVariable('GATEWAY_SECRET')

// ─── NMS Settings (production endpoints) ─────────────────────────────────────

param nmsBaseUrl = 'https://api-nms.aim.faa.gov/nmsapi'
param nmsAuthBaseUrl = 'https://api-nms.aim.faa.gov'
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
param resendEnabled = 'true'
param resendSegmentAllId = readEnvironmentVariable('RESEND_SEGMENT_ALL_ID', '')
param resendTopicAlertsId = readEnvironmentVariable('RESEND_TOPIC_ALERTS_ID', '')

// ─── Alerts ──────────────────────────────────────────────────────────────────

param alertsEnabled = true
param alertEmail = 'brandonberisford@gmail.com'

// ─── GitHub Deployment Identity ──────────────────────────────────────────────
// Object ID of the existing App Registration service principal used for
// GitHub Actions OIDC. Provides Contributor RBAC on the resource group.

param githubDeploymentPrincipalId = readEnvironmentVariable('GITHUB_DEPLOYMENT_PRINCIPAL_ID', '')

// ─── APIM Service Principal ─────────────────────────────────────────────────
// Object ID of the APIM management SP used by the frontend for subscription
// management and analytics.

param apimServicePrincipalId = readEnvironmentVariable('APIM_SERVICE_PRINCIPAL_ID', '')
