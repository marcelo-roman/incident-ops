@description('Azure region for the resources.')
param location string

@description('Tags applied to every resource.')
param tags object

@description('Logic App workflow name.')
param workflowName string

@description('Incoming webhook (Slack-compatible JSON) that receives on-call notifications; empty disables posting.')
@secure()
param webhookUrl string

resource workflow 'Microsoft.Logic/workflows@2019-05-01' = {
  name: workflowName
  location: location
  tags: tags
  properties: {
    state: 'Enabled'
    definition: loadJsonContent('../workflows/notify-oncall.json')
    parameters: {
      webhookUrl: {
        value: webhookUrl
      }
    }
  }
}

output workflowName string = workflow.name
