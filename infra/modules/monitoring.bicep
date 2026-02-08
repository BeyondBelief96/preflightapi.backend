@description('Azure region for all resources')
param location string

@description('Base name prefix for resources')
param baseName string

@description('Environment tag (test, prod)')
param environment string

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${baseName}-${environment}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${baseName}-${environment}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: 90
  }
}

@description('Application Insights connection string')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Application Insights instrumentation key')
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey

@description('Log Analytics workspace ID')
output logAnalyticsWorkspaceId string = logAnalytics.id
