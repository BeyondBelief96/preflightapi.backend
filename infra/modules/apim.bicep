@description('Azure region for all resources')
param location string

@description('Base name prefix for resources')
param baseName string

@description('Environment tag (test, prod)')
param environment string

@description('APIM publisher email')
param publisherEmail string

@description('APIM publisher name')
param publisherName string = 'PreflightApi'

@description('APIM SKU (Developer, Basic, Standard, Premium)')
param skuName string = 'Developer'

@description('APIM SKU capacity')
param skuCapacity int = 1

@description('Backend Web App hostname (e.g., preflightapi-eastus-web-api-prod.azurewebsites.net)')
param backendWebAppHostName string

var apimName = 'preflightapi-apim-service-${environment}'

// API Management Service
resource apim 'Microsoft.ApiManagement/service@2024-05-01' = {
  name: apimName
  location: location
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
  }
}

// API definition pointing to the backend Web App
resource api 'Microsoft.ApiManagement/service/apis@2024-05-01' = {
  parent: apim
  name: 'preflightapi'
  properties: {
    displayName: 'PreflightApi'
    path: ''
    protocols: ['https']
    serviceUrl: 'https://${backendWebAppHostName}'
    subscriptionRequired: true
    subscriptionKeyParameterNames: {
      header: 'Ocp-Apim-Subscription-Key'
      query: 'subscription-key'
    }
  }
}

@description('APIM service name')
output apimName string = apim.name

@description('APIM gateway URL')
output apimGatewayUrl string = apim.properties.gatewayUrl

@description('APIM resource ID')
output apimId string = apim.id
