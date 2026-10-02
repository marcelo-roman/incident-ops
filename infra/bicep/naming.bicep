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
@description('Power state: on deploys the messaging and the database and keeps one API replica warm; off removes them from the template and lets the API scale to zero.')
type environmentStateType = 'on' | 'off'

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

@export()
@description('Role definition ids the template assigns to workload identities.')
var workloadRoles = {
  serviceBusDataSender: '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
  serviceBusDataReceiver: '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'
  signalRAppServer: '420fcaa2-552c-430f-98ca-3264be4806c7'
  storageBlobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  cognitiveServicesOpenAiUser: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
}
