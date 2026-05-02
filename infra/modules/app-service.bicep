@description('Azure region for all resources')
param location string

@description('App Service Plan name')
param planName string

@description('Web App name')
param webAppName string

@description('App Service Plan SKU name (e.g., F1, B1, B2)')
param skuName string = 'B1'

@description('App Service Plan SKU tier (e.g., Free, Basic)')
param skuTier string = 'Basic'

@description('Web App platform (linux or windows)')
@allowed(['linux', 'windows'])
param webAppPlatform string = 'linux'

@description('Environment (test, prod)')
param environment string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('PostgreSQL server FQDN')
param databaseHost string

@description('PostgreSQL database name')
param databaseName string

@description('PostgreSQL username')
param databaseUsername string

@secure()
@description('PostgreSQL password')
param databasePassword string

@description('Storage account name for managed identity access')
param storageAccountName string

@description('Terminal procedures blob container name')
param terminalProceduresContainerName string

@description('Chart supplements blob container name')
param chartSupplementsContainerName string

@secure()
@description('NOAA API key for weather data')
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

// ─── Clerk Settings ─────────────────────────────────────────────────────────

@description('Clerk JWT authority URL')
param clerkAuthority string = ''

// ─── Custom Domain ──────────────────────────────────────────────────────────
// All optional. When customDomainHostName is empty the App Service runs only on
// its default *.azurewebsites.net hostname. When provided, an SNI SSL binding
// is created using the cert in Key Vault. DNS must already validate (CNAME or
// asuid TXT) before deploying with customDomainHostName set.

@description('Custom domain hostname (e.g., api.preflightapi.io). Leave empty to skip the binding.')
param customDomainHostName string = ''

@description('Key Vault resource ID for custom domain SSL cert lookup.')
param keyVaultId string = ''

@description('Name of the cert/secret in Key Vault for the custom domain. Leave empty to skip.')
param keyVaultCertificateName string = ''

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: webAppPlatform == 'linux' ? 'linux' : 'app'
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    reserved: webAppPlatform == 'linux'
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: webAppPlatform == 'linux' ? 'DOTNETCORE|8.0' : null
      netFrameworkVersion: webAppPlatform == 'windows' ? 'v8.0' : null
      alwaysOn: skuTier != 'Free'
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      httpLoggingEnabled: true
      detailedErrorLoggingEnabled: true
      requestTracingEnabled: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'Database__Host'
          value: databaseHost
        }
        {
          name: 'Database__Database'
          value: databaseName
        }
        {
          name: 'Database__Username'
          value: databaseUsername
        }
        {
          name: 'Database__Password'
          value: databasePassword
        }
        {
          name: 'Database__Port'
          value: '5432'
        }
        {
          name: 'CloudStorage__UseManagedIdentity'
          value: 'true'
        }
        {
          name: 'CloudStorage__AccountName'
          value: storageAccountName
        }
        {
          name: 'CloudStorage__TerminalProceduresContainerName'
          value: terminalProceduresContainerName
        }
        {
          name: 'CloudStorage__ChartSupplementsContainerName'
          value: chartSupplementsContainerName
        }
        {
          name: 'NOAASettings__NOAAApiKey'
          value: noaaApiKey
        }
        {
          name: 'ApiKeyAuth__BypassInDevelopment'
          value: environment == 'prod' ? 'false' : 'false'
        }
        {
          name: 'StripeSettings__SecretKey'
          value: stripeSecretKey
        }
        {
          name: 'StripeSettings__WebhookSecret'
          value: stripeWebhookSecret
        }
        {
          name: 'StripeSettings__PriceIdToTier__${stripePriceIdPrivatePilot}'
          value: !empty(stripePriceIdPrivatePilot) ? 'PrivatePilot' : ''
        }
        {
          name: 'StripeSettings__PriceIdToTier__${stripePriceIdCommercialPilot}'
          value: !empty(stripePriceIdCommercialPilot) ? 'CommercialPilot' : ''
        }
        {
          name: 'ClerkSettings__Authority'
          value: clerkAuthority
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'default'
        }
        {
          name: 'DIAGNOSTICS_AZUREBLOBRETENTIONINDAYS'
          value: '1'
        }
        {
          name: 'WEBSITE_HTTPLOGGING_RETENTION_DAYS'
          value: '1'
        }
      ]
    }
  }
}

@description('Web App name')
output webAppName string = webApp.name

@description('Web App default hostname')
output webAppHostName string = webApp.properties.defaultHostName

@description('Web App system-assigned managed identity principal ID')
output webAppPrincipalId string = webApp.identity.principalId

@description('Web App resource ID')
output webAppId string = webApp.id

@description('App Service Plan name')
output appServicePlanName string = appServicePlan.name

// ─── Custom Domain Binding (optional) ───────────────────────────────────────

var customDomainEnabled = !empty(customDomainHostName) && !empty(keyVaultCertificateName) && !empty(keyVaultId)

resource customDomainCert 'Microsoft.Web/certificates@2023-12-01' = if (customDomainEnabled) {
  name: keyVaultCertificateName
  location: location
  properties: {
    keyVaultId: keyVaultId
    keyVaultSecretName: keyVaultCertificateName
    serverFarmId: appServicePlan.id
  }
}

resource customDomainBinding 'Microsoft.Web/sites/hostNameBindings@2023-12-01' = if (customDomainEnabled) {
  parent: webApp
  name: customDomainHostName
  properties: {
    siteName: webApp.name
    sslState: 'SniEnabled'
    thumbprint: customDomainCert!.properties.thumbprint
  }
}
