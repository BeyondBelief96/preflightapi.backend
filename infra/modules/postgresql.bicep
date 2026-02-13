@description('Azure region for the PostgreSQL server')
param location string

@description('PostgreSQL flexible server name')
param serverName string

@description('PostgreSQL administrator login')
param administratorLogin string

@secure()
@description('PostgreSQL administrator password')
param administratorPassword string

@description('PostgreSQL database name')
param databaseName string = 'preflightapi'

@description('PostgreSQL SKU name (e.g., Standard_B1ms)')
param skuName string = 'Standard_B1ms'

@description('PostgreSQL SKU tier (Burstable, GeneralPurpose, MemoryOptimized)')
param skuTier string = 'Burstable'

@description('PostgreSQL storage size in GB')
param storageSizeGB int = 32

@description('PostgreSQL major version')
param version string = '16'

@description('Allow Azure services to access the server')
param allowAzureServices bool = true

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    version: version
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// Database
resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgresServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Enable PostGIS and PostGIS Topology extensions
resource postgisConfig 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = {
  parent: postgresServer
  name: 'azure.extensions'
  properties: {
    value: 'POSTGIS,POSTGIS_TOPOLOGY'
    source: 'user-override'
  }
}

// Firewall rule: Allow Azure services
resource allowAzureServicesRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = if (allowAzureServices) {
  parent: postgresServer
  name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

@description('PostgreSQL server fully qualified domain name')
output serverFqdn string = postgresServer.properties.fullyQualifiedDomainName

@description('PostgreSQL server name')
output serverName string = postgresServer.name

@description('PostgreSQL server resource ID')
output serverId string = postgresServer.id
