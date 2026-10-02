@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Flex Consumption plan name.')
param planName string

@description('Function app name.')
param functionAppName string

@description('Storage account used by the Functions host and for deployment packages.')
param storageAccountName string

@description('Application Insights component that receives telemetry.')
param appInsightsName string

@description('Service Bus fully qualified namespace for identity-based triggers.')
param serviceBusFullyQualifiedNamespace string

@description('Service Bus entity names consumed by the functions.')
param serviceBusEntities {
  topic: string
  slaSchedulerSubscription: string
  notifierSubscription: string
  slaChecksQueue: string
}

@description('Base URL of the incidents API.')
param incidentsApiBaseUrl string

@description('Base URL of the operations console used in notification links.')
param webBaseUrl string

@description('API key that authorizes the escalate endpoint.')
@secure()
param incidentsApiKey string

@description('Logic App workflow whose HTTP trigger receives on-call notifications.')
param notifyWorkflowName string

@description('Upper bound of instances the plan scales out to.')
@minValue(40)
@maxValue(1000)
param maximumInstanceCount int = 40

@description('Memory per instance in MB.')
@allowed([
  512
  2048
  4096
])
param instanceMemoryMB int = 2048

var deploymentContainerName = 'app-package'

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource notifyTrigger 'Microsoft.Logic/workflows/triggers@2019-05-01' existing = {
  name: '${notifyWorkflowName}/manual'
}

resource storage 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    publicNetworkAccess: 'Enabled'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2025-06-01' = {
  parent: storage
  name: 'default'
}

resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2025-06-01' = {
  parent: blobService
  name: deploymentContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: planName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2024-11-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentContainer.name}'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '8.0'
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storage.name
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ServiceBusConnection__fullyQualifiedNamespace'
          value: serviceBusFullyQualifiedNamespace
        }
        {
          name: 'IncidentEventsTopic'
          value: serviceBusEntities.topic
        }
        {
          name: 'SlaSchedulerSubscription'
          value: serviceBusEntities.slaSchedulerSubscription
        }
        {
          name: 'NotifierSubscription'
          value: serviceBusEntities.notifierSubscription
        }
        {
          name: 'SlaChecksQueue'
          value: serviceBusEntities.slaChecksQueue
        }
        {
          name: 'IncidentsApi__BaseUrl'
          value: incidentsApiBaseUrl
        }
        {
          name: 'IncidentsApi__ApiKey'
          value: incidentsApiKey
        }
        {
          name: 'Web__BaseUrl'
          value: webBaseUrl
        }
        {
          name: 'Notifications__LogicAppUrl'
          value: notifyTrigger.listCallbackUrl().value
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output defaultHostName string = functionApp.properties.defaultHostName
output principalId string = functionApp.identity.principalId
output storageAccountName string = storage.name
