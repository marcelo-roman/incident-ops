import { workloadRoles } from '../naming.bicep'

@description('Stable names of the identities that use Service Bus, used to derive role assignment ids.')
param identityNames {
  api: string
  functions: string
}

@description('Principal ids of the identities that use Service Bus.')
param principalIds {
  api: string
  functions: string
}

@description('Service Bus namespace name.')
param namespaceName string

@description('Topic the API publishes to.')
param topicName string

@description('Subscriptions the functions consume.')
param functionSubscriptionNames array

@description('Queue the functions schedule to and consume.')
param slaChecksQueueName string

resource namespace 'Microsoft.ServiceBus/namespaces@2026-01-01' existing = {
  name: namespaceName
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

resource apiTopicSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(topic.id, identityNames.api, workloadRoles.serviceBusDataSender)
  scope: topic
  properties: {
    principalId: principalIds.api
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', workloadRoles.serviceBusDataSender)
  }
}

resource functionsSubscriptionReceivers 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for (subscriptionName, index) in functionSubscriptionNames: {
    name: guid(functionSubscriptions[index].id, identityNames.functions, workloadRoles.serviceBusDataReceiver)
    scope: functionSubscriptions[index]
    properties: {
      principalId: principalIds.functions
      principalType: 'ServicePrincipal'
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', workloadRoles.serviceBusDataReceiver)
    }
  }
]

resource functionsQueueReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(slaChecksQueue.id, identityNames.functions, workloadRoles.serviceBusDataReceiver)
  scope: slaChecksQueue
  properties: {
    principalId: principalIds.functions
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', workloadRoles.serviceBusDataReceiver)
  }
}

resource functionsQueueSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(slaChecksQueue.id, identityNames.functions, workloadRoles.serviceBusDataSender)
  scope: slaChecksQueue
  properties: {
    principalId: principalIds.functions
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', workloadRoles.serviceBusDataSender)
  }
}
