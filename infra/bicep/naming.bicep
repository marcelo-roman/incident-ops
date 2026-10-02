@export()
@description('Resource names derived from the workload name and a deterministic suffix for globally unique resources.')
func resourceNames(workload string, suffix string) object => {
  logAnalytics: 'log-${workload}'
  appInsights: 'appi-${workload}'
  containerAppsEnvironment: 'cae-${workload}'
  apiContainerApp: 'ca-${workload}-api'
  insightsContainerApp: 'ca-${workload}-insights'
  sqlServer: 'sql-${workload}-${suffix}'
  sqlDatabase: 'sqldb-${workload}'
  signalR: 'sigr-${workload}-${suffix}'
  serviceBus: 'sbns-${workload}-${suffix}'
  functionPlan: 'asp-${workload}-functions'
  functionApp: 'func-${workload}'
  functionStorage: take('st${replace(workload, '-', '')}${suffix}', 24)
  logicApp: 'logic-${workload}-notify'
  openAi: 'oai-${workload}-${suffix}'
  staticWebApp: 'stapp-${workload}'
  budget: 'budget-${workload}'
}

@export()
@description('Custom domain lifecycle: None skips binding, Disabled adds the hostname and issues the managed certificate, SniEnabled binds the certificate.')
type customDomainBindingType = 'None' | 'Disabled' | 'SniEnabled'

@export()
@description('Public host names served through Cloudflare DNS.')
type hostNamesType = {
  web: string
  api: string
  insights: string
  docs: string
}

@export()
@description('Service Bus entities defined by the events contract.')
var serviceBusEntities = {
  topic: 'incident-events'
  slaSchedulerSubscription: 'sla-scheduler'
  notifierSubscription: 'notifier'
  slaChecksQueue: 'sla-checks'
}
