using 'main.bicep'

param owner = 'marcelo-roman'
param costCenter = 'portfolio'

param environmentState = readEnvironmentVariable('ENVIRONMENT_STATE', 'on')
param customDomainBinding = readEnvironmentVariable('CUSTOM_DOMAIN_BINDING', 'None')

param apiImage = readEnvironmentVariable('API_IMAGE', 'ghcr.io/marcelo-roman/incident-ops-api:latest')
param insightsImage = readEnvironmentVariable('INSIGHTS_IMAGE', 'ghcr.io/marcelo-roman/incident-ops-insights:latest')

param sqlEntraAdminLogin = readEnvironmentVariable('SQL_ADMIN_GROUP_NAME', 'sg-incident-ops-sql-admins')
param sqlEntraAdminObjectId = readEnvironmentVariable('SQL_ADMIN_GROUP_OBJECT_ID', '00000000-0000-0000-0000-000000000000')

param escalationApiKey = readEnvironmentVariable('ESCALATION_API_KEY')
param notificationWebhookUrl = readEnvironmentVariable('NOTIFICATION_WEBHOOK_URL', '')
param alertEmail = readEnvironmentVariable('ALERT_EMAIL', '')

param budgetAmount = 30
param budgetStartDate = '2026-10-01'
param budgetContactEmails = [
  readEnvironmentVariable('BUDGET_EMAIL', 'owner@example.com')
]
