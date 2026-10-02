@description('Stable names of the workload identities, used to derive role assignment ids.')
param identityNames {
  api: string
  insights: string
  functions: string
}

@description('Principal ids of the workload identities.')
param principalIds {
  api: string
  insights: string
  functions: string
}

@description('Service Bus namespace name.')
param serviceBusNamespaceName string

@description('Topic the API publishes to.')
param topicName string

@description('Subscriptions the functions consume.')
param functionSubscriptionNames array

@description('Queue the functions schedule to and consume.')
param slaChecksQueueName string

@description('SignalR Service name.')
param signalRName string

@description('Function host storage account name.')
param functionStorageAccountName string

@description('Azure OpenAI account name.')
param openAiAccountName string

var roles = {
  serviceBusDataSender: '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
  serviceBusDataReceiver: '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
  signalRAppServer: '420fcaa2-552c-430f-98ca-3264be4806c7'
  storageBlobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  cognitiveServicesOpenAiUser: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
}

resource namespace 'Microsoft.ServiceBus/namespaces@2026-01-01' existing = {
  name: serviceBusNamespaceName
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2026-01-01' existing = {
  parent: namespace
  name: topicName
}

resource functionSubscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2026-01-01' existing = [
  for subscriptionName in functionSubscriptionNames: {
    parent: topic
    name: subscriptionName
  }
]

resource slaChecksQueue 'Microsoft.ServiceBus/namespaces/queues@2026-01-01' existing = {
  parent: namespace
  name: slaChecksQueueName
}

resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
  name: signalRName
}

resource functionStorage 'Microsoft.Storage/storageAccounts@2025-06-01' existing = {
  name: functionStorageAccountName
}

resource openAi 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: openAiAccountName
}

resource apiTopicSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(topic.id, identityNames.api, roles.serviceBusDataSender)
  scope: topic
  properties: {
    principalId: principalIds.api
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataSender)
  }
}

resource apiSignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalR.id, identityNames.api, roles.signalRAppServer)
  scope: signalR
  properties: {
    principalId: principalIds.api
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.signalRAppServer)
  }
}

resource functionsSubscriptionReceivers 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for (subscriptionName, index) in functionSubscriptionNames: {
    name: guid(functionSubscriptions[index].id, identityNames.functions, roles.serviceBusDataReceiver)
    scope: functionSubscriptions[index]
    properties: {
      principalId: principalIds.functions
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataReceiver)
    }
  }
]

resource functionsQueueReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(slaChecksQueue.id, identityNames.functions, roles.serviceBusDataReceiver)
  scope: slaChecksQueue
  properties: {
    principalId: principalIds.functions
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataReceiver)
  }
}

resource functionsQueueSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(slaChecksQueue.id, identityNames.functions, roles.serviceBusDataSender)
  scope: slaChecksQueue
  properties: {
    principalId: principalIds.functions
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.serviceBusDataSender)
  }
}

resource functionsStorageOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(functionStorage.id, identityNames.functions, roles.storageBlobDataOwner)
  scope: functionStorage
  properties: {
    principalId: principalIds.functions
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageBlobDataOwner)
  }
}

resource insightsOpenAiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAi.id, identityNames.insights, roles.cognitiveServicesOpenAiUser)
  scope: openAi
  properties: {
    principalId: principalIds.insights
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.cognitiveServicesOpenAiUser)
  }
}

output assignedRoleDefinitionIds array = [
  roles.serviceBusDataSender
  roles.serviceBusDataReceiver
  roles.signalRAppServer
  roles.storageBlobDataOwner
  roles.cognitiveServicesOpenAiUser
]
