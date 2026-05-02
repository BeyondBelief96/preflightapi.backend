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

// ─── Custom Domain (App Service) ────────────────────────────────────────────
// Both values left empty intentionally. Fill in AFTER the DNS CNAME for
// api.preflightapi.io is flipped from APIM to the App Service default
// hostname (was-eastus-preflightapi-prd.azurewebsites.net) — Azure validates
// DNS before creating the binding, so this only deploys cleanly once DNS
// resolves to the App Service.
//
// To enable:
//   param customDomainHostName = 'api.preflightapi.io'
//   param keyVaultCertificateName = 'api-preflightapi-io'

param customDomainHostName = ''
param keyVaultCertificateName = ''

// ─── Secrets ─────────────────────────────────────────────────────────────────

param noaaApiKey = readEnvironmentVariable('NOAA_API_KEY')

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
param alertEmail = readEnvironmentVariable('ALERT_EMAIL', '')

// ─── GitHub Deployment Identity ──────────────────────────────────────────────
// Object ID of the existing App Registration service principal used for
// GitHub Actions OIDC. Provides Contributor RBAC on the resource group.

param githubDeploymentPrincipalId = readEnvironmentVariable('GITHUB_DEPLOYMENT_PRINCIPAL_ID', '')
