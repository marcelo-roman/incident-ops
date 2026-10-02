targetScope = 'resourceGroup'

import {
  resourceNames
  customDomainBindingType
  environmentStateType
  hostNamesType
  serviceBusEntities
  workloadRoles
} from 'naming.bicep'

metadata description = 'Incident Ops platform: telemetry, data, messaging, compute, AI and web hosting in one resource group.'

@description('Azure region for every regional resource.')
param location string = resourceGroup().location

@description('Azure region for the Azure SQL server and database; Azure SQL provisioning is restricted per region and subscription.')
param sqlLocation string = 'centralus'

@description('Workload name used to derive resource names.')
@minLength(3)
@maxLength(16)
param workload string = 'incident-ops'

@description('Owner tag value.')
param owner string

@description('Cost center tag value.')
param costCenter string

@description('Power state: on runs Service Bus, the SQL database, one warm API replica and the monitors; off removes Service Bus and the database from the template, scales the API to zero and idles the monitors.')
param environmentState environmentStateType = 'on'

@description('Public host names served through Cloudflare DNS.')
param hostNames hostNamesType = {
  web: 'incidents.marceloroman.com.br'
  api: 'incidents-api.marceloroman.com.br'
  insights: 'incidents-insights.marceloroman.com.br'
  docs: 'incidents-docs.marceloroman.com.br'
}

@description('GitHub Pages host that serves the documentation site.')
param githubPagesHost string = 'marcelo-roman.github.io'

@description('Custom domain lifecycle: None, then Disabled once DNS exists, then SniEnabled once certificates are issued.')
param customDomainBinding customDomainBindingType = 'None'

@description('Container image of incident-ops-api.')
param apiImage string

@description('Container image of incident-ops-insights.')
param insightsImage string

@description('Display name of the Microsoft Entra group that administers Azure SQL.')
param sqlEntraAdminLogin string

@description('Object id of the Microsoft Entra group that administers Azure SQL.')
param sqlEntraAdminObjectId string

@description('API key shared by the API and the functions for the escalate endpoint.')
@secure()
@minLength(32)
param escalationApiKey string

@description('Incoming webhook that receives on-call notifications; empty disables posting.')
@secure()
param notificationWebhookUrl string = ''

@description('Azure OpenAI deployment name the insights service addresses.')
param openAiDeploymentName string = 'rca-drafts'

@description('Azure OpenAI model name.')
param openAiModelName string = 'gpt-5.4-mini'

@description('Azure OpenAI model version.')
param openAiModelVersion string = '2026-03-17'

@description('Azure OpenAI deployment type; DataZoneStandard keeps processing within the US data zone.')
@allowed([
  'GlobalStandard'
  'DataZoneStandard'
  'Standard'
])
param openAiDeploymentSku string = 'DataZoneStandard'

@description('Azure OpenAI throughput in thousands of tokens per minute.')
@minValue(1)
param openAiCapacity int = 10

@description('Optional email address that receives every Azure Monitor alert; empty skips the receiver.')
param alertEmail string = ''

@description('Monthly budget amount in the billing currency.')
@minValue(1)
param budgetAmount int = 30

@description('First day of the month the budget starts tracking, as yyyy-MM-01.')
param budgetStartDate string

@description('Recipients of the budget alerts.')
@minLength(1)
param budgetContactEmails array

var names = resourceNames(workload, take(uniqueString(resourceGroup().id), 6), take(uniqueString(resourceGroup().id, sqlLocation), 6))
var tags = {
  project: workload
  owner: owner
  costCenter: costCenter
  environment: 'production'
  managedBy: 'bicep'
}
var isOn = environmentState == 'on'
var serviceBusFullyQualifiedNamespace = '${names.serviceBus}.servicebus.windows.net'
var customDomainsServed = customDomainBinding == 'SniEnabled'
var webOrigins = [
  'https://${hostNames.web}'
  'https://${staticWebApp.outputs.defaultHostName}'
]
var apiBaseUrl = customDomainsServed ? 'https://${hostNames.api}' : 'https://${api.outputs.fqdn}'
var webBaseUrl = customDomainsServed ? 'https://${hostNames.web}' : 'https://${staticWebApp.outputs.defaultHostName}'
var apiSecretName = 'escalation-api-key'

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    tags: tags
    logAnalyticsName: names.logAnalytics
    appInsightsName: names.appInsights
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    location: sqlLocation
    tags: tags
    serverName: names.sqlServer
    databaseName: names.sqlDatabase
    entraAdminLogin: sqlEntraAdminLogin
    entraAdminObjectId: sqlEntraAdminObjectId
    deployDatabase: isOn
  }
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp'
  params: {
    location: location
    tags: tags
    staticWebAppName: names.staticWebApp
    customDomainName: hostNames.web
    bindCustomDomain: customDomainBinding != 'None'
  }
}

module signalR 'modules/signalr.bicep' = {
  name: 'signalr'
  params: {
    location: location
    tags: tags
    signalRName: names.signalR
    allowedOrigins: webOrigins
  }
}

module serviceBus 'modules/servicebus.bicep' = if (isOn) {
  name: 'servicebus'
  params: {
    location: location
    tags: tags
    namespaceName: names.serviceBus
  }
}

module openAi 'modules/openai.bicep' = {
  name: 'openai'
  params: {
    location: location
    tags: tags
    accountName: names.openAi
    deploymentName: openAiDeploymentName
    modelName: openAiModelName
    modelVersion: openAiModelVersion
    deploymentSku: openAiDeploymentSku
    capacity: openAiCapacity
  }
}

module containerAppsEnvironment 'modules/containerapps-environment.bicep' = {
  name: 'containerapps-environment'
  params: {
    location: location
    tags: tags
    environmentName: names.containerAppsEnvironment
    logAnalyticsName: names.logAnalytics
  }
  dependsOn: [
    monitoring
  ]
}

module api 'modules/containerapp.bicep' = {
  name: 'containerapp-api'
  params: {
    location: location
    tags: tags
    containerAppName: names.apiContainerApp
    environmentName: names.containerAppsEnvironment
    image: apiImage
    targetPort: 8080
    livenessPath: '/health/live'
    readinessPath: '/health/ready'
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: isOn ? 1 : 0
    customDomainName: hostNames.api
    customDomainBinding: customDomainBinding
    secrets: {
      '${apiSecretName}': escalationApiKey
    }
    secretEnvironmentVariables: [
      {
        name: 'Security__EscalationApiKey'
        secretRef: apiSecretName
      }
    ]
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'OTEL_SERVICE_NAME'
        value: 'incident-ops-api'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: monitoring.outputs.appInsightsConnectionString
      }
      {
        name: 'Database__ApplyMigrations'
        value: 'true'
      }
      {
        name: 'Database__Seed'
        value: 'true'
      }
      {
        name: 'ConnectionStrings__IncidentOps'
        value: 'Server=tcp:${sql.outputs.serverFqdn},1433;Database=${sql.outputs.databaseName};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connect Timeout=60;'
      }
      {
        name: 'ServiceBus__FullyQualifiedNamespace'
        value: serviceBusFullyQualifiedNamespace
      }
      {
        name: 'ServiceBus__TopicName'
        value: serviceBusEntities.topic
      }
      {
        name: 'Azure__SignalR__ConnectionString'
        value: 'Endpoint=https://${signalR.outputs.hostName};AuthType=azure.msi;Version=1.0;'
      }
      {
        name: 'Cors__AllowedOrigins__0'
        value: webOrigins[0]
      }
      {
        name: 'Cors__AllowedOrigins__1'
        value: webOrigins[1]
      }
    ]
  }
  dependsOn: [
    containerAppsEnvironment
  ]
}

module insights 'modules/containerapp.bicep' = {
  name: 'containerapp-insights'
  params: {
    location: location
    tags: tags
    containerAppName: names.insightsContainerApp
    environmentName: names.containerAppsEnvironment
    image: insightsImage
    targetPort: 8000
    livenessPath: '/health'
    readinessPath: '/health'
    cpu: '0.5'
    memory: '1Gi'
    customDomainName: hostNames.insights
    customDomainBinding: customDomainBinding
    environmentVariables: [
      {
        name: 'OTEL_SERVICE_NAME'
        value: 'incident-ops-insights'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: monitoring.outputs.appInsightsConnectionString
      }
      {
        name: 'INCIDENTS_API_BASE_URL'
        value: apiBaseUrl
      }
      {
        name: 'AZURE_OPENAI_ENDPOINT'
        value: openAi.outputs.endpoint
      }
      {
        name: 'AZURE_OPENAI_DEPLOYMENT'
        value: openAi.outputs.deploymentName
      }
      {
        name: 'CORS_ALLOWED_ORIGINS'
        value: join(webOrigins, ',')
      }
    ]
  }
  dependsOn: [
    containerAppsEnvironment
  ]
}

module logicApp 'modules/logicapp.bicep' = {
  name: 'logicapp'
  params: {
    location: location
    tags: tags
    workflowName: names.logicApp
    webhookUrl: notificationWebhookUrl
  }
}

module functions 'modules/functions.bicep' = {
  name: 'functions'
  params: {
    location: location
    tags: tags
    planName: names.functionPlan
    functionAppName: names.functionApp
    storageAccountName: names.functionStorage
    appInsightsName: names.appInsights
    serviceBusFullyQualifiedNamespace: serviceBusFullyQualifiedNamespace
    serviceBusEntities: serviceBusEntities
    incidentsApiBaseUrl: apiBaseUrl
    incidentsApiKey: escalationApiKey
    webBaseUrl: webBaseUrl
    notifyWorkflowName: names.logicApp
  }
  dependsOn: [
    logicApp
  ]
}

module roleAssignments 'modules/roleassignments.bicep' = {
  name: 'roleassignments'
  params: {
    identityNames: {
      api: names.apiContainerApp
      insights: names.insightsContainerApp
      functions: names.functionApp
    }
    principalIds: {
      api: api.outputs.principalId
      insights: insights.outputs.principalId
      functions: functions.outputs.principalId
    }
    signalRName: names.signalR
    functionStorageAccountName: names.functionStorage
    openAiAccountName: names.openAi
  }
}

module serviceBusRoleAssignments 'modules/servicebus-roleassignments.bicep' = if (isOn) {
  name: 'servicebus-roleassignments'
  params: {
    identityNames: {
      api: names.apiContainerApp
      functions: names.functionApp
    }
    principalIds: {
      api: api.outputs.principalId
      functions: functions.outputs.principalId
    }
    namespaceName: names.serviceBus
    topicName: serviceBusEntities.topic
    functionSubscriptionNames: [
      serviceBusEntities.slaSchedulerSubscription
      serviceBusEntities.notifierSubscription
    ]
    slaChecksQueueName: serviceBusEntities.slaChecksQueue
  }
  dependsOn: [
    serviceBus
  ]
}

module alerting 'modules/alerting.bicep' = {
  name: 'alerting'
  params: {
    location: location
    tags: tags
    appInsightsName: names.appInsights
    serviceBusNamespaceName: names.serviceBus
    apiRoleName: 'incident-ops-api'
    functionRoleName: names.functionApp
    healthUrl: '${apiBaseUrl}/health/live'
    enabled: isOn
    monitorServiceBus: isOn
    alertWebhookBaseUrl: apiBaseUrl
    alertWebhookKey: escalationApiKey
    alertEmail: alertEmail
  }
  dependsOn: [
    serviceBus
  ]
}

module budget 'modules/budget.bicep' = {
  name: 'budget'
  params: {
    budgetName: names.budget
    amount: budgetAmount
    startDate: budgetStartDate
    contactEmails: budgetContactEmails
  }
}

@description('Default host names before custom domains are bound.')
output defaultHostNames object = {
  web: staticWebApp.outputs.defaultHostName
  api: api.outputs.fqdn
  insights: insights.outputs.fqdn
  functions: functions.outputs.defaultHostName
}

@description('Custom host names served once customDomainBinding is SniEnabled.')
output customHostNames hostNamesType = hostNames

@description('Value of the asuid TXT records that prove ownership of the container app host names.')
output containerAppsVerificationId string = containerAppsEnvironment.outputs.customDomainVerificationId

@description('DNS records to create in Cloudflare, DNS-only.')
output dnsRecords array = [
  {
    type: 'CNAME'
    name: hostNames.web
    content: staticWebApp.outputs.defaultHostName
  }
  {
    type: 'CNAME'
    name: hostNames.api
    content: api.outputs.fqdn
  }
  {
    type: 'TXT'
    name: 'asuid.${hostNames.api}'
    content: containerAppsEnvironment.outputs.customDomainVerificationId
  }
  {
    type: 'CNAME'
    name: hostNames.insights
    content: insights.outputs.fqdn
  }
  {
    type: 'TXT'
    name: 'asuid.${hostNames.insights}'
    content: containerAppsEnvironment.outputs.customDomainVerificationId
  }
  {
    type: 'CNAME'
    name: hostNames.docs
    content: githubPagesHost
  }
]

@description('Resource names that delivery workflows target.')
output deliveryTargets object = {
  resourceGroup: resourceGroup().name
  apiContainerApp: api.outputs.containerAppName
  insightsContainerApp: insights.outputs.containerAppName
  functionApp: functions.outputs.functionAppName
  staticWebApp: staticWebApp.outputs.staticWebAppName
}

@description('Data plane endpoints for operators.')
output endpoints object = {
  sqlServer: sql.outputs.serverFqdn
  sqlDatabase: sql.outputs.databaseName
  serviceBus: serviceBusFullyQualifiedNamespace
  signalR: signalR.outputs.hostName
  openAi: openAi.outputs.endpoint
  openAiDeployment: openAi.outputs.deploymentName
  logAnalyticsWorkspaceId: monitoring.outputs.workspaceCustomerId
}

@description('Role definition ids assigned to workload identities, for the deployment principal ABAC condition.')
output assignedRoleDefinitionIds array = map(items(workloadRoles), role => role.value)

@description('Power state this deployment applied.')
output environmentState environmentStateType = environmentState
