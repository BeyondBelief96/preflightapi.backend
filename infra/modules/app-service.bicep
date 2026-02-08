@description('Azure region for all resources')
param location string

@description('Base name prefix for resources')
param baseName string

@description('Environment tag (test, prod)')
param environment string

@description('App Service Plan SKU name (e.g., B2)')
param skuName string = 'B2'

@description('App Service Plan SKU tier (e.g., Basic)')
param skuTier string = 'Basic'

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

@description('Airport diagrams blob container name')
param airportDiagramsContainerName string

@description('Chart supplements blob container name')
param chartSupplementsContainerName string

@secure()
@description('NOAA API key for weather services')
param noaaApiKey string

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
@description('APIM-to-API shared secret for gateway validation')
param gatewaySecret string

var appServicePlanName = 'asp-${baseName}-api-${environment}'
var webAppName = 'preflightapi-${baseName}-web-api-${environment}'

// App Service Plan (Linux)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    reserved: true // Required for Linux
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
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
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
          name: 'CloudStorage__AirportDiagramsContainerName'
          value: airportDiagramsContainerName
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
          name: 'GatewaySecret'
          value: gatewaySecret
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
