@description('Azure region for the regional resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Application Insights component that runs the availability test and holds request telemetry.')
param appInsightsName string

@description('Service Bus namespace watched for dead-lettered messages.')
param serviceBusNamespaceName string

@description('Cloud role name the Function App reports to Application Insights.')
param functionRoleName string

@description('Cloud role name the API reports to Application Insights.')
param apiRoleName string

@description('Liveness URL probed by the availability test.')
param healthUrl string

@description('Runs the availability test and evaluates every alert rule; false keeps them deployed and idle.')
param enabled bool

@description('Deploys the dead-letter alert, which needs the Service Bus namespace to exist.')
param monitorServiceBus bool

@description('Incidents API base URL that receives Azure Monitor alerts.')
param alertWebhookBaseUrl string

@description('API key appended as the code query parameter of the alert webhook.')
@secure()
param alertWebhookKey string

@description('Optional email address that also receives every alert; empty skips the receiver.')
param alertEmail string = ''

@description('Availability test locations.')
@minLength(1)
param testLocations array = [
  'us-va-ash-azr'
]

@description('Seconds between availability test runs.')
@allowed([
  300
  600
  900
])
param testFrequencySeconds int = 900

@description('Locations that must fail at the same time before the availability alert fires.')
@minValue(1)
param failedLocationThreshold int = 1

@description('Failed API requests in five minutes that raise the failure alert.')
@minValue(1)
param failedRequestsThreshold int = 5

@description('Average API server response time in milliseconds that raises the latency alert.')
@minValue(100)
param responseTimeThresholdMs int = 2000

var serviceProperty = 'platform'
var availabilityTestName = 'webtest-${apiRoleName}-live'
var hasEmail = !empty(alertEmail)
var webhookReceivers = [
  {
    name: 'incident-ops-api'
    serviceUri: '${alertWebhookBaseUrl}/api/alerts/azure-monitor?code=${alertWebhookKey}'
    useCommonAlertSchema: true
    useAadAuth: false
  }
]
var emailReceivers = hasEmail
  ? [
      {
        name: 'operator-email'
        emailAddress: alertEmail
        useCommonAlertSchema: true
      }
    ]
  : []

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2026-01-01' existing = {
  name: serviceBusNamespaceName
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-incident-ops'
  location: 'Global'
  tags: tags
  properties: {
    groupShortName: 'incidentops'
    enabled: true
    webhookReceivers: webhookReceivers
    emailReceivers: emailReceivers
  }
}

resource availabilityTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: availabilityTestName
  location: location
  tags: union(tags, {
    'hidden-link:${appInsights.id}': 'Resource'
  })
  kind: 'standard'
  properties: {
    SyntheticMonitorId: availabilityTestName
    Name: 'API liveness'
    Description: 'GET ${healthUrl} expecting 200'
    Enabled: enabled
    Kind: 'standard'
    Frequency: testFrequencySeconds
    Timeout: 30
    RetryEnabled: true
    Locations: [
      for locationId in testLocations: {
        Id: locationId
      }
    ]
    Request: {
      RequestUrl: healthUrl
      HttpVerb: 'GET'
      ParseDependentRequests: false
    }
    ValidationRules: {
      ExpectedHttpStatusCode: 200
      SSLCheck: true
      SSLCertRemainingLifetimeCheck: 7
    }
  }
}

resource availabilityAlert 'Microsoft.Insights/metricAlerts@2026-01-01' = {
  name: 'alert-${apiRoleName}-availability'
  location: 'global'
  tags: union(tags, {
    'hidden-link:${appInsights.id}': 'Resource'
    'hidden-link:${availabilityTest.id}': 'Resource'
  })
  properties: {
    description: 'API liveness check is failing from ${failedLocationThreshold} or more locations.'
    severity: 1
    enabled: enabled
    scopes: [
      availabilityTest.id
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.WebtestLocationAvailabilityCriteria'
      webTestId: availabilityTest.id
      componentId: appInsights.id
      failedLocationCount: failedLocationThreshold
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
        webHookProperties: {
          service: serviceProperty
          component: apiRoleName
        }
      }
    ]
  }
}

resource failedRequestsAlert 'Microsoft.Insights/metricAlerts@2026-01-01' = {
  name: 'alert-${apiRoleName}-failed-requests'
  location: 'global'
  tags: tags
  properties: {
    description: 'API returned more than ${failedRequestsThreshold} failed requests in five minutes.'
    severity: 2
    enabled: enabled
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'FailedRequests'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'requests/failed'
          dimensions: [
            {
              name: 'cloud/roleName'
              operator: 'Include'
              values: [
                apiRoleName
              ]
            }
          ]
          operator: 'GreaterThan'
          threshold: failedRequestsThreshold
          timeAggregation: 'Count'
          skipMetricValidation: true
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
        webHookProperties: {
          service: serviceProperty
          component: apiRoleName
        }
      }
    ]
  }
}

resource responseTimeAlert 'Microsoft.Insights/metricAlerts@2026-01-01' = {
  name: 'alert-${apiRoleName}-response-time'
  location: 'global'
  tags: tags
  properties: {
    description: 'API average server response time is above ${responseTimeThresholdMs} ms over fifteen minutes.'
    severity: 3
    enabled: enabled
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'ServerResponseTime'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'requests/duration'
          dimensions: [
            {
              name: 'cloud/roleName'
              operator: 'Include'
              values: [
                apiRoleName
              ]
            }
          ]
          operator: 'GreaterThan'
          threshold: responseTimeThresholdMs
          timeAggregation: 'Average'
          skipMetricValidation: true
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
        webHookProperties: {
          service: serviceProperty
          component: apiRoleName
        }
      }
    ]
  }
}

resource deadLetterAlert 'Microsoft.Insights/metricAlerts@2026-01-01' = if (monitorServiceBus) {
  name: 'alert-${serviceBus.name}-dead-letters'
  location: 'global'
  tags: tags
  properties: {
    description: 'Messages reached a dead-letter queue.'
    severity: 2
    enabled: enabled
    scopes: [
      serviceBus.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'DeadletteredMessages'
          metricNamespace: 'Microsoft.ServiceBus/namespaces'
          metricName: 'DeadletteredMessages'
          dimensions: [
            {
              name: 'EntityName'
              operator: 'Include'
              values: [
                '*'
              ]
            }
          ]
          operator: 'GreaterThan'
          threshold: 0
          timeAggregation: 'Maximum'
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
        webHookProperties: {
          service: serviceProperty
          component: 'service-bus'
        }
      }
    ]
  }
}

resource functionFailuresAlert 'Microsoft.Insights/scheduledQueryRules@2026-03-01' = {
  name: 'alert-${functionRoleName}-failures'
  location: location
  tags: tags
  kind: 'LogAlert'
  properties: {
    displayName: 'Function executions failing'
    description: 'At least one function execution failed in the last fifteen minutes.'
    severity: 3
    enabled: enabled
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT15M'
    windowSize: 'PT15M'
    criteria: {
      allOf: [
        {
          query: 'requests | where cloud_RoleName =~ \'${functionRoleName}\' and success == false | summarize failures = count() by operation_Name'
          timeAggregation: 'Total'
          metricMeasureColumn: 'failures'
          dimensions: [
            {
              name: 'operation_Name'
              operator: 'Include'
              values: [
                '*'
              ]
            }
          ]
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [
        actionGroup.id
      ]
      customProperties: {
        service: serviceProperty
        component: functionRoleName
      }
    }
  }
}

output actionGroupId string = actionGroup.id
output availabilityTestName string = availabilityTest.name
output alertRuleNames array = concat(
  [
    availabilityAlert.name
    failedRequestsAlert.name
    responseTimeAlert.name
    functionFailuresAlert.name
  ],
  monitorServiceBus ? [deadLetterAlert.name] : []
)
