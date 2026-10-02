@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Logical SQL server name.')
param serverName string

@description('Database name.')
param databaseName string

@description('Display name of the Microsoft Entra principal that administers the server.')
param entraAdminLogin string

@description('Object id of the Microsoft Entra principal that administers the server.')
param entraAdminObjectId string

@description('Principal type of the Microsoft Entra administrator.')
@allowed([
  'Group'
  'User'
  'Application'
])
param entraAdminPrincipalType string = 'Group'

@description('Deploys the database; false leaves only the logical server, which has no compute cost.')
param deployDatabase bool

@description('DTUs of the Basic database.')
@allowed([
  5
])
param basicDtu int = 5

@description('Maximum database size in bytes; 2 GB is the Basic ceiling.')
param maxSizeBytes int = 2147483648

resource server 'Microsoft.Sql/servers@2025-01-01' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    restrictOutboundNetworkAccess: 'Disabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: entraAdminLogin
      sid: entraAdminObjectId
      principalType: entraAdminPrincipalType
      tenantId: tenant().tenantId
    }
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2025-01-01' = {
  parent: server
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2025-01-01' = if (deployDatabase) {
  parent: server
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: basicDtu
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: maxSizeBytes
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
}

output serverName string = server.name
output serverFqdn string = server.properties.fullyQualifiedDomainName
output databaseName string = databaseName
