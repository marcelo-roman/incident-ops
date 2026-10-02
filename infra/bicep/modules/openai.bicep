@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Azure OpenAI account name, also used as the custom subdomain.')
param accountName string

@description('Deployment name the application addresses.')
param deploymentName string

@description('Model name in the Azure OpenAI catalog.')
param modelName string

@description('Model version in the Azure OpenAI catalog.')
param modelVersion string

@description('Deployment type.')
@allowed([
  'GlobalStandard'
  'DataZoneStandard'
  'Standard'
])
param deploymentSku string = 'GlobalStandard'

@description('Throughput in thousands of tokens per minute.')
@minValue(1)
param capacity int = 10

resource account 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: accountName
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: accountName
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
  }
}

resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: account
  name: deploymentName
  sku: {
    name: deploymentSku
    capacity: capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
    versionUpgradeOption: 'OnceCurrentVersionExpired'
  }
}

output accountName string = account.name
output endpoint string = account.properties.endpoint
output deploymentName string = deployment.name
