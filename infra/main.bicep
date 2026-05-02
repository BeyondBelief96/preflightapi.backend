targetScope = 'subscription'

// ─── Environment ─────────────────────────────────────────────────────────────

@description('Primary Azure region for resources')
param location string

@description('Environment name (test, prod)')
@allowed(['test', 'prod'])
param environment string

// ─── Resource Names ──────────────────────────────────────────────────────────
// Fully parameterized — each environment specifies exact names matching what's
// already deployed. No names are derived, so there's no naming drift between
// Bicep and what exists in the portal.

@description('Resource group name')
param resourceGroupName string

@description('Log Analytics workspace name')
param logAnalyticsName string

@description('Application Insights resource name for the API')
param apiAppInsightsName string

@description('Application Insights resource name for the Function App')
param functionAppInsightsName string

@description('PostgreSQL flexible server name')
param postgresServerName string

@description('Azure region for PostgreSQL (can differ from primary location)')
param postgresLocation string = location

@description('Storage account name for blob data (globally unique, 3-24 lowercase alphanumeric)')
param storageAccountName string

@description('Terminal procedures blob container name')
param terminalProceduresContainerName string

@description('Chart supplements blob container name')
param chartSupplementsContainerName string

@description('PreflightApi resources blob container name')
param preflightApiResourcesContainerName string

@description('App Service Plan name for the API')
param appServicePlanName string

@description('Web App name for the API')
param webAppName string

@description('Web App platform (linux or windows)')
@allowed(['linux', 'windows'])
param webAppPlatform string = 'linux'

@description('Function App Flex Consumption plan name')
param functionsPlanName string

@description('Function App name')
param functionAppName string

@description('Storage account name for Functions runtime (leave empty to share the data account)')
param functionsStorageName string = ''

@description('Key Vault name')
param keyVaultName string

// ─── PostgreSQL ──────────────────────────────────────────────────────────────

@description('PostgreSQL administrator login')
param dbAdminLogin string

@secure()
@description('PostgreSQL administrator password')
param dbAdminPassword string

@description('PostgreSQL database name')
param databaseName string = 'preflightapi'

@description('PostgreSQL SKU name')
param dbSkuName string = 'Standard_B1ms'

@description('PostgreSQL SKU tier')
param dbSkuTier string = 'Burstable'

@description('PostgreSQL storage size in GB')
param dbStorageSizeGB int = 32

@description('PostgreSQL major version')
param dbVersion string = '16'

// ─── Storage ─────────────────────────────────────────────────────────────────

@description('Storage account SKU')
param storageSkuName string = 'Standard_LRS'

// ─── App Service ─────────────────────────────────────────────────────────────

@description('App Service Plan SKU name')
param apiSkuName string = 'B1'

@description('App Service Plan SKU tier')
param apiSkuTier string = 'Basic'

// ─── Custom Domain (App Service) ────────────────────────────────────────────

@description('Custom domain hostname for the App Service (e.g., api.preflightapi.io). Leave empty to skip.')
param customDomainHostName string = ''

@description('Certificate name in Key Vault for the App Service custom domain. Leave empty to skip.')
param keyVaultCertificateName string = ''

// ─── Secrets ─────────────────────────────────────────────────────────────────

@secure()
@description('NOAA API key for weather data (used by API and Functions)')
param noaaApiKey string

// ─── Stripe Settings ────────────────────────────────────────────────────────

@secure()
@description('Stripe secret API key')
param stripeSecretKey string = ''

@secure()
@description('Stripe webhook signing secret')
param stripeWebhookSecret string = ''

@description('Stripe Price ID for Private Pilot tier')
param stripePriceIdPrivatePilot string = ''

@description('Stripe Price ID for Commercial Pilot tier')
param stripePriceIdCommercialPilot string = ''

// ─── NMS Settings (Azure Functions) ──────────────────────────────────────────

@description('NMS API base URL')
param nmsBaseUrl string

@description('NMS OAuth2 auth base URL')
param nmsAuthBaseUrl string

@secure()
@description('NMS OAuth2 client ID')
param nmsClientId string

@secure()
@description('NMS OAuth2 client secret')
param nmsClientSecret string

// ─── Clerk Settings (optional) ───────────────────────────────────────────────

@description('Clerk JWT authority URL (leave empty to omit from app settings)')
param clerkAuthority string = ''

@secure()
@description('Clerk secret key (leave empty to omit)')
param clerkSecretKey string = ''

// ─── Certificate Renewal (Azure Functions) ──────────────────────────────────

@description('ACME email for certificate renewal')
param certificateAcmeEmail string = ''

@description('Certificate name in Key Vault')
param certificateCertName string = ''

@description('Domain for certificate renewal')
param certificateDomain string = ''

@description('Root domain for DNS challenge')
param certificateRootDomain string = ''

// ─── Porkbun DNS (Azure Functions) ──────────────────────────────────────────

@secure()
@description('Porkbun API key')
param porkbunApiKey string = ''

@secure()
@description('Porkbun secret API key')
param porkbunSecretApiKey string = ''

// ─── Resend Email (Azure Functions) ─────────────────────────────────────────

@secure()
@description('Resend API token')
param resendApiToken string = ''

@description('Enable Resend email notifications')
param resendEnabled string = 'false'

@description('Resend from address')
param resendFromAddress string = 'alerts@contact.preflightapi.io'

@description('Quiet period in minutes between alerts')
param resendQuietPeriodMinutes string = '1440'

@description('Resend reply-to address')
param resendReplyToAddress string = 'bberisford@preflightapi.io'

@description('Resend segment ID for all users')
param resendSegmentAllId string = ''

@description('Resend topic ID for alerts')
param resendTopicAlertsId string = ''

// ─── Alerts ─────────────────────────────────────────────────────────────────

@description('Enable Azure Monitor alerts (production only)')
param alertsEnabled bool = false

@description('Email address for alert notifications')
param alertEmail string = ''

// ─── GitHub Deployment Identity ──────────────────────────────────────────────
// Provide the Object ID of your GitHub deployment service principal (App
// Registration or Managed Identity). Used for Contributor RBAC on the resource
// group. Leave empty to skip the role assignment.

@description('Object ID of the GitHub deployment service principal')
param githubDeploymentPrincipalId string = ''

// ─── Resource Group ──────────────────────────────────────────────────────────

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

// ─── Modules ─────────────────────────────────────────────────────────────────

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-${environment}'
  scope: rg
  params: {
    location: location
    logAnalyticsName: logAnalyticsName
    apiAppInsightsName: apiAppInsightsName
    functionAppInsightsName: functionAppInsightsName
  }
}

module postgresql 'modules/postgresql.bicep' = {
  name: 'postgresql-${environment}'
  scope: rg
  params: {
    location: postgresLocation
    serverName: postgresServerName
    administratorLogin: dbAdminLogin
    administratorPassword: dbAdminPassword
    databaseName: databaseName
    skuName: dbSkuName
    skuTier: dbSkuTier
    storageSizeGB: dbStorageSizeGB
    version: dbVersion
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage-${environment}'
  scope: rg
  params: {
    location: location
    storageAccountName: storageAccountName
    skuName: storageSkuName
    terminalProceduresContainerName: terminalProceduresContainerName
    chartSupplementsContainerName: chartSupplementsContainerName
    preflightApiResourcesContainerName: preflightApiResourcesContainerName
    functionsStorageName: functionsStorageName
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault-${environment}'
  scope: rg
  params: {
    location: location
    keyVaultName: keyVaultName
  }
}

module appService 'modules/app-service.bicep' = {
  name: 'app-service-${environment}'
  scope: rg
  params: {
    location: location
    planName: appServicePlanName
    webAppName: webAppName
    skuName: apiSkuName
    skuTier: apiSkuTier
    webAppPlatform: webAppPlatform
    environment: environment
    appInsightsConnectionString: monitoring.outputs.apiAppInsightsConnectionString
    databaseHost: postgresql.outputs.serverFqdn
    databaseName: databaseName
    databaseUsername: dbAdminLogin
    databasePassword: dbAdminPassword
    storageAccountName: storage.outputs.storageAccountName
    terminalProceduresContainerName: storage.outputs.terminalProceduresContainerName
    chartSupplementsContainerName: storage.outputs.chartSupplementsContainerName
    noaaApiKey: noaaApiKey
    stripeSecretKey: stripeSecretKey
    stripeWebhookSecret: stripeWebhookSecret
    stripePriceIdPrivatePilot: stripePriceIdPrivatePilot
    stripePriceIdCommercialPilot: stripePriceIdCommercialPilot
    clerkAuthority: clerkAuthority
    customDomainHostName: customDomainHostName
    keyVaultId: keyVault.outputs.keyVaultId
    keyVaultCertificateName: keyVaultCertificateName
  }
}

module functionApp 'modules/function-app.bicep' = {
  name: 'function-app-${environment}'
  scope: rg
  params: {
    location: location
    planName: functionsPlanName
    functionAppName: functionAppName
    functionsStorageAccountName: storage.outputs.functionsStorageAccountName
    appInsightsConnectionString: monitoring.outputs.functionAppInsightsConnectionString
    databaseHost: postgresql.outputs.serverFqdn
    databaseName: databaseName
    databaseUsername: dbAdminLogin
    databasePassword: dbAdminPassword
    storageAccountName: storage.outputs.storageAccountName
    terminalProceduresContainerName: storage.outputs.terminalProceduresContainerName
    chartSupplementsContainerName: storage.outputs.chartSupplementsContainerName
    preflightApiResourcesContainerName: storage.outputs.preflightApiResourcesContainerName
    nmsBaseUrl: nmsBaseUrl
    nmsAuthBaseUrl: nmsAuthBaseUrl
    nmsClientId: nmsClientId
    nmsClientSecret: nmsClientSecret
    noaaApiKey: noaaApiKey
    clerkAuthority: clerkAuthority
    clerkSecretKey: clerkSecretKey
    certificateAcmeEmail: certificateAcmeEmail
    certificateCertName: certificateCertName
    certificateDomain: certificateDomain
    certificateKeyVaultName: keyVault.outputs.keyVaultName
    certificateRootDomain: certificateRootDomain
    porkbunApiKey: porkbunApiKey
    porkbunSecretApiKey: porkbunSecretApiKey
    resendApiToken: resendApiToken
    resendEnabled: resendEnabled
    resendFromAddress: resendFromAddress
    resendHealthEndpointUrl: 'https://${appService.outputs.webAppHostName}/health'
    resendQuietPeriodMinutes: resendQuietPeriodMinutes
    resendReplyToAddress: resendReplyToAddress
    resendSegmentAllId: resendSegmentAllId
    resendTopicAlertsId: resendTopicAlertsId
  }
}

module roleAssignments 'modules/role-assignments.bicep' = {
  name: 'role-assignments-${environment}'
  scope: rg
  params: {
    storageAccountName: storage.outputs.storageAccountName
    webAppPrincipalId: appService.outputs.webAppPrincipalId
    functionAppPrincipalId: functionApp.outputs.functionAppPrincipalId
    githubDeploymentPrincipalId: githubDeploymentPrincipalId
    keyVaultName: keyVault.outputs.keyVaultName
    postgresServerName: postgresql.outputs.serverName
  }
}

module alerts 'modules/alerts.bicep' = if (alertsEnabled) {
  name: 'alerts-${environment}'
  scope: rg
  params: {
    location: location
    alertEmail: alertEmail
    webAppId: appService.outputs.webAppId
    functionAppInsightsId: monitoring.outputs.functionAppInsightsId
  }
}

// ─── Outputs ─────────────────────────────────────────────────────────────────

@description('Resource group name')
output resourceGroupName string = rg.name

@description('PostgreSQL server FQDN')
output postgresServerFqdn string = postgresql.outputs.serverFqdn

@description('PostgreSQL server name')
output postgresServerName string = postgresql.outputs.serverName

@description('Web App name')
output webAppName string = appService.outputs.webAppName

@description('Web App hostname')
output webAppHostName string = appService.outputs.webAppHostName

@description('Function App name')
output functionAppName string = functionApp.outputs.functionAppName

@description('Function App hostname')
output functionAppHostName string = functionApp.outputs.functionAppHostName

@description('Storage account name')
output storageAccountName string = storage.outputs.storageAccountName

@description('Functions storage account name')
output functionsStorageAccountName string = storage.outputs.functionsStorageAccountName

@description('API Application Insights connection string')
output apiAppInsightsConnectionString string = monitoring.outputs.apiAppInsightsConnectionString

@description('Function App Application Insights connection string')
output functionAppInsightsConnectionString string = monitoring.outputs.functionAppInsightsConnectionString

@description('Key Vault name')
output keyVaultName string = keyVault.outputs.keyVaultName

@description('Key Vault URI')
output keyVaultUri string = keyVault.outputs.keyVaultUri
