@description('Azure region for all resources')
param location string

@description('Function App Flex Consumption plan name')
param planName string

@description('Function App name')
param functionAppName string

@description('Functions storage account name (created by storage module)')
param functionsStorageAccountName string

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

@description('PreflightApi resources blob container name')
param preflightApiResourcesContainerName string

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

@secure()
@description('NOAA API key for weather data synchronization')
param noaaApiKey string

@secure()
@description('APIM-to-API shared secret for gateway validation')
param gatewaySecret string

@description('Clerk JWT authority URL (leave empty to omit)')
param clerkAuthority string = ''

@secure()
@description('Clerk secret key (leave empty to omit)')
param clerkSecretKey string = ''

// ─── Certificate Renewal Settings ───────────────────────────────────────────

@description('ACME email for certificate renewal')
param certificateAcmeEmail string = ''

@description('Certificate name in Key Vault')
param certificateCertName string = ''

@description('Domain for certificate renewal')
param certificateDomain string = ''

@description('Key Vault name for certificate storage')
param certificateKeyVaultName string = ''

@description('Root domain for DNS challenge')
param certificateRootDomain string = ''

// ─── Porkbun DNS Settings ───────────────────────────────────────────────────

@secure()
@description('Porkbun API key')
param porkbunApiKey string = ''

@secure()
@description('Porkbun secret API key')
param porkbunSecretApiKey string = ''

// ─── Resend Email Settings ──────────────────────────────────────────────────

@secure()
@description('Resend API token')
param resendApiToken string = ''

@description('Enable Resend email notifications')
param resendEnabled string = 'false'

@description('Resend from address')
param resendFromAddress string = 'alerts@contact.preflightapi.io'

@description('Health endpoint URL for outage monitoring')
param resendHealthEndpointUrl string = ''

@description('Quiet period in minutes between alerts')
param resendQuietPeriodMinutes string = '1440'

@description('Resend reply-to address')
param resendReplyToAddress string = 'bberisford@preflightapi.io'

@description('Resend segment ID for all users')
param resendSegmentAllId string = ''

@description('Resend topic ID for alerts')
param resendTopicAlertsId string = ''

// ─── Resources ──────────────────────────────────────────────────────────────

// Reference the Functions storage account (created by storage module or pre-existing)
resource functionsStorage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: functionsStorageAccountName
}

// Flex Consumption Plan
resource functionsPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'functionapp'
  sku: {
    tier: 'FlexConsumption'
    name: 'FC1'
  }
  properties: {
    reserved: true
  }
}

// Build app settings — conditionally include Clerk and certificate settings
var baseAppSettings = [
  {
    name: 'AzureWebJobsStorage'
    value: 'DefaultEndpointsProtocol=https;AccountName=${functionsStorage.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${functionsStorage.listKeys().keys[0].value}'
  }
  {
    name: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
    value: 'DefaultEndpointsProtocol=https;AccountName=${functionsStorage.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${functionsStorage.listKeys().keys[0].value}'
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
    name: 'CloudStorage__PreflightApiResourcesContainerName'
    value: preflightApiResourcesContainerName
  }
  {
    name: 'NmsSettings__BaseUrl'
    value: nmsBaseUrl
  }
  {
    name: 'NmsSettings__AuthBaseUrl'
    value: nmsAuthBaseUrl
  }
  {
    name: 'NmsSettings__ClientId'
    value: nmsClientId
  }
  {
    name: 'NmsSettings__ClientSecret'
    value: nmsClientSecret
  }
  {
    name: 'NOAASettings__NOAAApiKey'
    value: noaaApiKey
  }
  {
    name: 'GatewaySecret'
    value: gatewaySecret
  }
]

var clerkSettings = empty(clerkAuthority) && empty(clerkSecretKey) ? [] : concat(
  empty(clerkAuthority) ? [] : [
    {
      name: 'ClerkSettings__Authority'
      value: clerkAuthority
    }
    {
      name: 'ClerkSettings__RequireAuthenticationInDevelopment'
      value: 'true'
    }
  ],
  empty(clerkSecretKey) ? [] : [
    {
      name: 'ClerkSettings__SecretKey'
      value: clerkSecretKey
    }
  ]
)

var certificateSettings = empty(certificateAcmeEmail) ? [] : [
  {
    name: 'CertificateRenewal__AcmeEmail'
    value: certificateAcmeEmail
  }
  {
    name: 'CertificateRenewal__CertificateName'
    value: certificateCertName
  }
  {
    name: 'CertificateRenewal__Domain'
    value: certificateDomain
  }
  {
    name: 'CertificateRenewal__KeyVaultName'
    value: certificateKeyVaultName
  }
  {
    name: 'CertificateRenewal__RootDomain'
    value: certificateRootDomain
  }
]

var porkbunSettings = empty(porkbunApiKey) ? [] : [
  {
    name: 'Porkbun__ApiKey'
    value: porkbunApiKey
  }
  {
    name: 'Porkbun__SecretApiKey'
    value: porkbunSecretApiKey
  }
]

var resendSettings = empty(resendApiToken) ? [] : [
  {
    name: 'Resend__ApiToken'
    value: resendApiToken
  }
  {
    name: 'Resend__Enabled'
    value: resendEnabled
  }
  {
    name: 'Resend__FromAddress'
    value: resendFromAddress
  }
  {
    name: 'Resend__HealthEndpointUrl'
    value: resendHealthEndpointUrl
  }
  {
    name: 'Resend__QuietPeriodMinutes'
    value: resendQuietPeriodMinutes
  }
  {
    name: 'Resend__ReplyToAddress'
    value: resendReplyToAddress
  }
  {
    name: 'Resend__SegmentAllId'
    value: resendSegmentAllId
  }
  {
    name: 'Resend__TopicAlertsId'
    value: resendTopicAlertsId
  }
]

// Function App
resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: functionsPlan.id
    httpsOnly: true
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: 'https://${functionsStorageAccountName}.blob.${az.environment().suffixes.storage}/app-package-${functionAppName}'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '8.0'
      }
      scaleAndConcurrency: {
        instanceMemoryMB: 4096
        maximumInstanceCount: 100
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: concat(baseAppSettings, clerkSettings, certificateSettings, porkbunSettings, resendSettings)
    }
  }
}

// ─── Outputs ────────────────────────────────────────────────────────────────

@description('Function App name')
output functionAppName string = functionApp.name

@description('Function App default hostname')
output functionAppHostName string = functionApp.properties.defaultHostName

@description('Function App system-assigned managed identity principal ID')
output functionAppPrincipalId string = functionApp.identity.principalId

@description('Function App resource ID')
output functionAppId string = functionApp.id
