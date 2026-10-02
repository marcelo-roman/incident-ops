@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Container Apps environment name.')
param environmentName string

@description('Log Analytics workspace that receives console and system logs.')
param logAnalyticsName string

resource workspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: logAnalyticsName
}

resource environment 'Microsoft.App/managedEnvironments@2025-07-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    zoneRedundant: false
  }
}

output environmentId string = environment.id
output environmentName string = environment.name
output defaultDomain string = environment.properties.defaultDomain
output customDomainVerificationId string = environment.properties.customDomainConfiguration.customDomainVerificationId
