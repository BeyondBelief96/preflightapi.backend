@description('Azure region for all resources')
param location string

@description('API Management service name')
param apimName string

@description('APIM publisher email')
param publisherEmail string

@description('APIM publisher name')
param publisherName string = 'PreflightApi'

@description('APIM SKU name (Developer, BasicV2, StandardV2, etc.)')
param skuName string = 'BasicV2'

@description('APIM SKU capacity')
param skuCapacity int = 1

@description('Backend Web App hostname (e.g., was-eastus-preflightapi-prd.azurewebsites.net)')
param backendWebAppHostName string

@secure()
@description('APIM-to-API shared secret for gateway validation')
param gatewaySecret string

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

// Named value for gateway secret (referenced by api-policy.xml as {{apim-gateway-secret}})
resource gatewaySecretNamedValue 'Microsoft.ApiManagement/service/namedValues@2024-05-01' = {
  parent: apim
  name: 'apim-gateway-secret'
  properties: {
    displayName: 'apim-gateway-secret'
    value: gatewaySecret
    secret: true
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

// API-level policy (caching, gateway secret injection)
resource apiPolicy 'Microsoft.ApiManagement/service/apis/policies@2024-05-01' = {
  parent: api
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('../../apim-policies/api-policy.xml')
  }
  dependsOn: [
    gatewaySecretNamedValue
  ]
}

// ─── Products ─────────────────────────────────────────────────────────────────

// Student Pilot (Free tier)
resource studentPilotProduct 'Microsoft.ApiManagement/service/products@2024-05-01' = {
  parent: apim
  name: 'student-pilot'
  properties: {
    displayName: 'Student Pilot'
    description: 'Free tier — basic weather and airport data'
    subscriptionRequired: true
    approvalRequired: false
    state: 'published'
  }
}

resource studentPilotApiLink 'Microsoft.ApiManagement/service/products/apis@2024-05-01' = {
  parent: studentPilotProduct
  name: 'preflightapi'
  dependsOn: [
    api
  ]
}

resource studentPilotPolicy 'Microsoft.ApiManagement/service/products/policies@2024-05-01' = {
  parent: studentPilotProduct
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('../../apim-policies/student-pilot-policy.xml')
  }
}

// Private Pilot (Starter tier)
resource privatePilotProduct 'Microsoft.ApiManagement/service/products@2024-05-01' = {
  parent: apim
  name: 'private-pilot'
  properties: {
    displayName: 'Private Pilot'
    description: 'Starter tier — weather, airports, airspace, and more'
    subscriptionRequired: true
    approvalRequired: false
    state: 'published'
  }
}

resource privatePilotApiLink 'Microsoft.ApiManagement/service/products/apis@2024-05-01' = {
  parent: privatePilotProduct
  name: 'preflightapi'
  dependsOn: [
    api
  ]
}

resource privatePilotPolicy 'Microsoft.ApiManagement/service/products/policies@2024-05-01' = {
  parent: privatePilotProduct
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('../../apim-policies/private-pilot-policy.xml')
  }
}

// Commercial Pilot (Professional tier)
resource commercialPilotProduct 'Microsoft.ApiManagement/service/products@2024-05-01' = {
  parent: apim
  name: 'commercial-pilot'
  properties: {
    displayName: 'Commercial Pilot'
    description: 'Professional tier — full access to all endpoints'
    subscriptionRequired: true
    approvalRequired: false
    state: 'published'
  }
}

resource commercialPilotApiLink 'Microsoft.ApiManagement/service/products/apis@2024-05-01' = {
  parent: commercialPilotProduct
  name: 'preflightapi'
  dependsOn: [
    api
  ]
}

resource commercialPilotPolicy 'Microsoft.ApiManagement/service/products/policies@2024-05-01' = {
  parent: commercialPilotProduct
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('../../apim-policies/commercial-pilot-policy.xml')
  }
}

// ATP (Enterprise tier)
resource atpProduct 'Microsoft.ApiManagement/service/products@2024-05-01' = {
  parent: apim
  name: 'atp'
  properties: {
    displayName: 'ATP'
    description: 'Enterprise tier — full access to all endpoints with highest limits'
    subscriptionRequired: true
    approvalRequired: false
    state: 'published'
  }
}

resource atpApiLink 'Microsoft.ApiManagement/service/products/apis@2024-05-01' = {
  parent: atpProduct
  name: 'preflightapi'
  dependsOn: [
    api
  ]
}

resource atpPolicy 'Microsoft.ApiManagement/service/products/policies@2024-05-01' = {
  parent: atpProduct
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('../../apim-policies/atp-policy.xml')
  }
}

// ─── Outputs ──────────────────────────────────────────────────────────────────

@description('APIM service name')
output apimName string = apim.name

@description('APIM gateway URL')
output apimGatewayUrl string = apim.properties.gatewayUrl

@description('APIM resource ID')
output apimId string = apim.id
