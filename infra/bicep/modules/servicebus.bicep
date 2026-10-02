import { serviceBusEntities } from '../naming.bicep'

@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Service Bus namespace name.')
param namespaceName string

@description('Deliveries attempted before a message moves to the dead-letter queue.')
@minValue(1)
@maxValue(100)
param maxDeliveryCount int = 10

var escalationTriggerFilter = 'eventType IN (\'incident.triggered\',\'incident.escalated\')'
var subscriptions = [
  {
    name: serviceBusEntities.slaSchedulerSubscription
    ruleName: 'triggered-or-escalated'
    filter: escalationTriggerFilter
  }
  {
    name: serviceBusEntities.notifierSubscription
    ruleName: 'paging-severities'
    filter: '${escalationTriggerFilter} AND severity IN (\'Sev1\',\'Sev2\')'
  }
]

resource namespace 'Microsoft.ServiceBus/namespaces@2026-01-01' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    disableLocalAuth: true
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource topic 'Microsoft.ServiceBus/namespaces/topics@2026-01-01' = {
  parent: namespace
  name: serviceBusEntities.topic
  properties: {
    defaultMessageTimeToLive: 'P14D'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    supportOrdering: true
    enableBatchedOperations: true
  }
}

resource topicSubscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2026-01-01' = [
  for subscription in subscriptions: {
    parent: topic
    name: subscription.name
    properties: {
      lockDuration: 'PT1M'
      maxDeliveryCount: maxDeliveryCount
      defaultMessageTimeToLive: 'P14D'
      deadLetteringOnMessageExpiration: true
      deadLetteringOnFilterEvaluationExceptions: true
      enableBatchedOperations: true
    }
  }
]

resource subscriptionFilters 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2026-01-01' = [
  for (subscription, index) in subscriptions: {
    parent: topicSubscriptions[index]
    name: subscription.ruleName
    properties: {
      filterType: 'SqlFilter'
      sqlFilter: {
        sqlExpression: subscription.filter
      }
    }
  }
]

resource slaChecksQueue 'Microsoft.ServiceBus/namespaces/queues@2026-01-01' = {
  parent: namespace
  name: serviceBusEntities.slaChecksQueue
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: maxDeliveryCount
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    maxSizeInMegabytes: 1024
    enableBatchedOperations: true
  }
}

output namespaceName string = namespace.name
output fullyQualifiedNamespace string = replace(replace(namespace.properties.serviceBusEndpoint, 'https://', ''), ':443/', '')
output topicName string = topic.name
output slaSchedulerSubscriptionName string = topicSubscriptions[0].name
output notifierSubscriptionName string = topicSubscriptions[1].name
output slaChecksQueueName string = slaChecksQueue.name
