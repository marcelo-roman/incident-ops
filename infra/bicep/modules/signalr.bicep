@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('SignalR Service name.')
param signalRName string

@description('Browser origins allowed to negotiate with the hub.')
param allowedOrigins array

resource signalR 'Microsoft.SignalRService/signalR@2024-03-01' = {
  name: signalRName
  location: location
  tags: tags
  kind: 'SignalR'
  sku: {
    name: 'Free_F1'
    tier: 'Free'
    capacity: 1
  }
  properties: {
    features: [
      {
        flag: 'ServiceMode'
        value: 'Default'
      }
      {
        flag: 'EnableConnectivityLogs'
        value: 'True'
      }
    ]
    cors: {
      allowedOrigins: allowedOrigins
    }
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
    tls: {
      clientCertEnabled: false
    }
  }
}

output signalRName string = signalR.name
output hostName string = signalR.properties.hostName
