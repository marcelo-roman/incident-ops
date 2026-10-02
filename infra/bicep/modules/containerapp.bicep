import { customDomainBindingType } from '../naming.bicep'

@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Container app name.')
param containerAppName string

@description('Container Apps environment name.')
param environmentName string

@description('Public container image, including tag or digest.')
param image string

@description('Port the container listens on.')
param targetPort int

@description('HTTP path for the liveness probe.')
param livenessPath string

@description('HTTP path for the readiness probe.')
param readinessPath string

@description('Plain environment variables as name/value pairs.')
param environmentVariables array = []

@description('Secrets exposed to the container as secretRef environment variables.')
@secure()
param secrets object = {}

@description('Environment variables that read from secrets, as name/secretRef pairs.')
param secretEnvironmentVariables array = []

@description('vCPU per replica.')
param cpu string = '0.25'

@description('Memory per replica.')
param memory string = '0.5Gi'

@description('Maximum replica count.')
@minValue(1)
@maxValue(10)
param maxReplicas int = 2

@description('Concurrent HTTP requests per replica before scaling out.')
param concurrentRequests int = 50

@description('Custom host name bound to the app; empty skips the binding.')
param customDomainName string = ''

@description('Custom domain lifecycle stage.')
param customDomainBinding customDomainBindingType = 'None'

var hasCustomDomain = !empty(customDomainName) && customDomainBinding != 'None'
var certificateName = 'mc-${take(replace(customDomainName, '.', '-'), 60)}'
var secretList = [
  for secretName in objectKeys(secrets): {
    name: secretName
    value: secrets[secretName]
  }
]
var probeDefaults = {
  timeoutSeconds: 5
  failureThreshold: 3
}

resource environment 'Microsoft.App/managedEnvironments@2025-07-01' existing = {
  name: environmentName
}

resource containerApp 'Microsoft.App/containerApps@2025-07-01' = {
  name: containerAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: secretList
      ingress: {
        external: true
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
        customDomains: hasCustomDomain
          ? [
              {
                name: customDomainName
                bindingType: customDomainBinding
                certificateId: customDomainBinding == 'SniEnabled'
                  ? resourceId('Microsoft.App/managedEnvironments/managedCertificates', environment.name, certificateName)
                  : null
              }
            ]
          : []
      }
    }
    template: {
      containers: [
        {
          name: containerAppName
          image: image
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: concat(environmentVariables, secretEnvironmentVariables)
          probes: [
            union(probeDefaults, {
              type: 'Startup'
              httpGet: {
                path: livenessPath
                port: targetPort
              }
              initialDelaySeconds: 3
              periodSeconds: 5
              failureThreshold: 30
            })
            union(probeDefaults, {
              type: 'Liveness'
              httpGet: {
                path: livenessPath
                port: targetPort
              }
              periodSeconds: 30
            })
            union(probeDefaults, {
              type: 'Readiness'
              httpGet: {
                path: readinessPath
                port: targetPort
              }
              periodSeconds: 10
            })
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-concurrency'
            http: {
              metadata: {
                concurrentRequests: string(concurrentRequests)
              }
            }
          }
        ]
      }
    }
  }
}

resource managedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2025-07-01' = if (hasCustomDomain) {
  parent: environment
  name: certificateName
  location: location
  tags: tags
  properties: {
    subjectName: customDomainName
    domainControlValidation: 'CNAME'
  }
  dependsOn: [
    containerApp
  ]
}

output containerAppName string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output principalId string = containerApp.identity.principalId
