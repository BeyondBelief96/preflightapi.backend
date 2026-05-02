using '../main.bicep'

param location = 'eastus'
param environment = 'test'

// ─── Resource Names (matching actual TST infrastructure) ─────────────────────

param resourceGroupName = 'rg-preflightapi-eastus-test'
param logAnalyticsName = 'preflightapi-log-analytics-workspace-eastus-tst'
param apiAppInsightsName = 'preflightapi-eastus-web-api-test'
param functionAppInsightsName = 'az-func-preflightapi-eastus-test'
param postgresServerName = 'pgsql-preflightapi-eastus-test'
param postgresLocation = 'eastus2'
param storageAccountName = 'rgpreflightapieastusad'
param terminalProceduresContainerName = 'preflightapi-terminal-procedures-centralus-test'
param chartSupplementsContainerName = 'preflightapi-chart-supplements-centralus-test'
param preflightApiResourcesContainerName = 'sa-eastus-container-preflightapi-resources-tst'
param appServicePlanName = 'ASP-rgpreflightapieastustest-ac44'
param webAppName = 'preflightapi-eastus-web-api-test'
param webAppPlatform = 'windows'
param functionsPlanName = 'ASP-rgpreflightapieastustest-9578'
param functionAppName = 'az-func-preflightapi-eastus-test'
param functionsStorageName = '' // TST uses a single storage account for both data and functions
param keyVaultName = 'KeyVaultPreflightApiTest'

// ─── PostgreSQL ──────────────────────────────────────────────────────────────

param dbAdminLogin = 'preflightapi_admin'
param dbAdminPassword = readEnvironmentVariable('DB_ADMIN_PASSWORD')
param databaseName = 'preflightapi-eastus-test-database'
param dbSkuName = 'Standard_B1ms'
param dbSkuTier = 'Burstable'
param dbStorageSizeGB = 32
param dbVersion = '15'

// ─── Storage ─────────────────────────────────────────────────────────────────

param storageSkuName = 'Standard_RAGRS'

// ─── App Service ─────────────────────────────────────────────────────────────

param apiSkuName = 'F1'
param apiSkuTier = 'Free'

// ─── Custom Domain (App Service) ────────────────────────────────────────────
// Test environment uses the default *.azurewebsites.net hostname; no binding.

param customDomainHostName = ''
param keyVaultCertificateName = ''

// ─── Secrets ─────────────────────────────────────────────────────────────────

param noaaApiKey = readEnvironmentVariable('NOAA_API_KEY')

// ─── Stripe Settings ────────────────────────────────────────────────────────

param stripeSecretKey = readEnvironmentVariable('STRIPE_SECRET_KEY', '')
param stripeWebhookSecret = readEnvironmentVariable('STRIPE_WEBHOOK_SECRET', '')
param stripePriceIdPrivatePilot = readEnvironmentVariable('STRIPE_PRICE_ID_PRIVATE_PILOT', '')
param stripePriceIdCommercialPilot = readEnvironmentVariable('STRIPE_PRICE_ID_COMMERCIAL_PILOT', '')

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
