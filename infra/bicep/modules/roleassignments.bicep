import { workloadRoles } from '../naming.bicep'

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

@description('SignalR Service name.')
param signalRName string

@description('Function host storage account name.')
param functionStorageAccountName string

@description('Azure OpenAI account name.')
param openAiAccountName string

resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' existing = {
  name: signalRName
}

resource functionStorage 'Microsoft.Storage/storageAccounts@2025-06-01' existing = {
  name: functionStorageAccountName
}

resource openAi 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: openAiAccountName
}

resource apiSignalRAppServer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(signalR.id, identityNames.api, workloadRoles.signalRAppServer)
  scope: signalR
  properties: {
    principalId: principalIds.api
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', workloadRoles.signalRAppServer)
  }
}

resource functionsStorageOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(functionStorage.id, identityNames.functions, workloadRoles.storageBlobDataOwner)
  scope: functionStorage
  properties: {
    principalId: principalIds.functions
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', workloadRoles.storageBlobDataOwner)
  }
}

resource insightsOpenAiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAi.id, identityNames.insights, workloadRoles.cognitiveServicesOpenAiUser)
  scope: openAi
  properties: {
    principalId: principalIds.insights
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', workloadRoles.cognitiveServicesOpenAiUser)
  }
}
