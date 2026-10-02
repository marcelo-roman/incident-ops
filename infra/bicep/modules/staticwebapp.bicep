@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Static Web App name.')
param staticWebAppName string

@description('Custom host name; empty skips the binding.')
param customDomainName string = ''

@description('Adds the custom domain once its CNAME points at the default host name.')
param bindCustomDomain bool = false

resource staticWebApp 'Microsoft.Web/staticSites@2024-11-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Disabled'
    publicNetworkAccess: 'Enabled'
  }
}

resource customDomain 'Microsoft.Web/staticSites/customDomains@2024-11-01' = if (bindCustomDomain && !empty(customDomainName)) {
  parent: staticWebApp
  name: customDomainName
  properties: {
    validationMethod: 'cname-delegation'
  }
}

output staticWebAppName string = staticWebApp.name
output defaultHostName string = staticWebApp.properties.defaultHostname
