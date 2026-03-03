@description('Azure region for all resources')
param location string

@description('Log Analytics workspace name')
param logAnalyticsName string

@description('Application Insights resource name for the API')
param apiAppInsightsName string

@description('Application Insights resource name for the Function App')
param functionAppInsightsName string

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights — API
resource apiAppInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: apiAppInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: 90
  }
}

// Application Insights — Function App
resource functionAppInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: functionAppInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: 90
  }
}

@description('API Application Insights connection string')
output apiAppInsightsConnectionString string = apiAppInsights.properties.ConnectionString

@description('Function App Application Insights connection string')
output functionAppInsightsConnectionString string = functionAppInsights.properties.ConnectionString

@description('Log Analytics workspace ID')
output logAnalyticsWorkspaceId string = logAnalytics.id

@description('Log Analytics workspace name')
output logAnalyticsWorkspaceName string = logAnalytics.name
