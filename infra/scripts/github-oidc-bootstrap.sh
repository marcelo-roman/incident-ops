#!/usr/bin/env bash
set -euo pipefail

readonly LOCATION="${LOCATION:-eastus2}"
readonly RESOURCE_GROUP="${RESOURCE_GROUP:-rg-incident-ops}"
readonly APP_NAME="${APP_NAME:-sp-incident-ops-github}"
readonly SQL_ADMIN_GROUP="${SQL_ADMIN_GROUP:-sg-incident-ops-sql-admins}"
readonly GITHUB_OWNER="${GITHUB_OWNER:-marcelo-roman}"
readonly GITHUB_REPOSITORY_NAME="${GITHUB_REPOSITORY_NAME:-incident-ops}"
readonly REPOSITORY="${GITHUB_OWNER}/${GITHUB_REPOSITORY_NAME}"
readonly GITHUB_API="${GITHUB_API:-https://api.github.com}"
readonly ISSUER="https://token.actions.githubusercontent.com"
readonly AUDIENCE="api://AzureADTokenExchange"
readonly WORKLOAD_ROLE_IDS=(
  69a216fc-b8fb-44d8-bc22-1f3c2cd27a39
  4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0
  420fcaa2-552c-430f-98ca-3264be4806c7
  b7e6dc6d-f1e8-4753-8033-0f276bb0955b
  5e0bd9bd-7b93-4f28-af87-19fc36ad61bd
)
readonly CONTRIBUTOR_ROLE_ID="b24988ac-6180-42a0-ab88-20f7382dd24c"
readonly RBAC_ADMIN_ROLE_ID="f58310d9-a9f6-439a-9e8d-f62e7b41a168"

log() {
  echo "==> $*" >&2
}

ensure_resource_group() {
  log "resource group ${RESOURCE_GROUP} in ${LOCATION}"
  az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --tags project=incident-ops owner="$GITHUB_OWNER" costCenter=portfolio \
    --query id --output tsv
}

ensure_app() {
  local app_id
  app_id=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" --output tsv)
  if [[ -n "$app_id" ]]; then
    log "app registration ${APP_NAME} exists"
    echo "$app_id"
    return
  fi
  log "creating app registration ${APP_NAME}"
  az ad app create --display-name "$APP_NAME" --sign-in-audience AzureADMyOrg --query appId --output tsv
}

ensure_service_principal() {
  local app_id="$1"
  local object_id
  object_id=$(az ad sp list --filter "appId eq '${app_id}'" --query "[0].id" --output tsv)
  if [[ -n "$object_id" ]]; then
    log "service principal exists"
    echo "$object_id"
    return
  fi
  log "creating service principal"
  az ad sp create --id "$app_id" --query id --output tsv
}

ensure_federated_credential() {
  local app_id="$1"
  local name="$2"
  local subject="$3"
  local existing
  existing=$(az ad app federated-credential list --id "$app_id" --query "[?name=='${name}'] | length(@)" --output tsv)
  if [[ "$existing" != "0" ]]; then
    log "federated credential ${name} exists"
    return
  fi
  log "federated credential ${name} -> ${subject}"
  az ad app federated-credential create --id "$app_id" --parameters "$(
    jq -nc --arg name "$name" --arg issuer "$ISSUER" --arg subject "$subject" --arg audience "$AUDIENCE" \
      '{name: $name, issuer: $issuer, subject: $subject, audiences: [$audience]}'
  )" --output none
}

subject_prefix() {
  local owner_id repository_id
  owner_id=$(curl --silent --show-error --fail "${GITHUB_API}/users/${GITHUB_OWNER}" | jq -r .id)
  repository_id=$(curl --silent --show-error --fail "${GITHUB_API}/repos/${REPOSITORY}" | jq -r .id)
  echo "repo:${GITHUB_OWNER}@${owner_id}/${GITHUB_REPOSITORY_NAME}@${repository_id}"
}

ensure_federated_credentials() {
  local app_id="$1"
  local prefix
  prefix=$(subject_prefix)
  ensure_federated_credential "$app_id" "${GITHUB_REPOSITORY_NAME}-production-id" "${prefix}:environment:production"
  ensure_federated_credential "$app_id" "${GITHUB_REPOSITORY_NAME}-main-id" "${prefix}:ref:refs/heads/main"
  ensure_federated_credential "$app_id" "${GITHUB_REPOSITORY_NAME}-power-id" "${prefix}:environment:power"
}

rbac_condition() {
  local guids
  guids=$(IFS=,; echo "${WORKLOAD_ROLE_IDS[*]}")
  guids="${guids//,/, }"
  printf '%s' "((!(ActionMatches{'Microsoft.Authorization/roleAssignments/write'})) OR (@Request[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {${guids}} AND @Request[Microsoft.Authorization/roleAssignments:PrincipalType] ForAnyOfAnyValues:StringEqualsIgnoreCase {'ServicePrincipal'})) AND ((!(ActionMatches{'Microsoft.Authorization/roleAssignments/delete'})) OR (@Resource[Microsoft.Authorization/roleAssignments:RoleDefinitionId] ForAnyOfAnyValues:GuidEquals {${guids}} AND @Resource[Microsoft.Authorization/roleAssignments:PrincipalType] ForAnyOfAnyValues:StringEqualsIgnoreCase {'ServicePrincipal'}))"
}

ensure_role() {
  local object_id="$1"
  local role_id="$2"
  local scope="$3"
  local condition="${4:-}"
  local existing
  existing=$(az role assignment list --assignee "$object_id" --role "$role_id" --scope "$scope" --query "length(@)" --output tsv)
  if [[ "$existing" != "0" ]]; then
    log "role ${role_id} already assigned"
    return
  fi
  log "assigning role ${role_id}"
  if [[ -z "$condition" ]]; then
    az role assignment create \
      --assignee-object-id "$object_id" \
      --assignee-principal-type ServicePrincipal \
      --role "$role_id" \
      --scope "$scope" \
      --output none
    return
  fi
  az role assignment create \
    --assignee-object-id "$object_id" \
    --assignee-principal-type ServicePrincipal \
    --role "$role_id" \
    --scope "$scope" \
    --condition "$condition" \
    --condition-version "2.0" \
    --output none
}

ensure_group_member() {
  local group_id="$1"
  local member_id="$2"
  local label="$3"
  if [[ "$(az ad group member check --group "$group_id" --member-id "$member_id" --query value --output tsv)" == "true" ]]; then
    log "${label} is a member of ${SQL_ADMIN_GROUP}"
    return
  fi
  log "adding ${label} to ${SQL_ADMIN_GROUP}"
  az ad group member add --group "$group_id" --member-id "$member_id"
}

ensure_sql_admin_group() {
  local service_principal_id="$1"
  local group_id user_id
  group_id=$(az ad group list --display-name "$SQL_ADMIN_GROUP" --query "[0].id" --output tsv)
  if [[ -z "$group_id" ]]; then
    log "creating group ${SQL_ADMIN_GROUP}"
    group_id=$(az ad group create --display-name "$SQL_ADMIN_GROUP" --mail-nickname "$SQL_ADMIN_GROUP" --query id --output tsv)
  fi
  user_id=$(az ad signed-in-user show --query id --output tsv)
  ensure_group_member "$group_id" "$user_id" "signed-in user"
  ensure_group_member "$group_id" "$service_principal_id" "service principal ${APP_NAME}"
  echo "$group_id"
}

print_github_commands() {
  local app_id="$1"
  local tenant_id="$2"
  local group_id="$3"
  echo
  echo "# Repository variables for OIDC login and the production environment with a required reviewer"
  echo "REVIEWER_ID=\$(gh api users/${GITHUB_OWNER} --jq .id)"
  echo "gh variable set AZURE_CLIENT_ID --repo ${REPOSITORY} --body ${app_id}"
  echo "gh variable set AZURE_TENANT_ID --repo ${REPOSITORY} --body ${tenant_id}"
  echo "gh variable set AZURE_SUBSCRIPTION_ID --repo ${REPOSITORY} --body ${SUBSCRIPTION_ID}"
  echo "gh api --method PUT repos/${REPOSITORY}/environments/production -F 'reviewers[][type]=User' -F \"reviewers[][id]=\${REVIEWER_ID}\" -F 'deployment_branch_policy[protected_branches]=true' -F 'deployment_branch_policy[custom_branch_policies]=false'"
  echo "gh api --method PUT repos/${REPOSITORY}/environments/power -F 'deployment_branch_policy[protected_branches]=true' -F 'deployment_branch_policy[custom_branch_policies]=false'"
  echo
  echo "# Infrastructure deployment inputs"
  echo "gh variable set SQL_ADMIN_GROUP_NAME --repo ${REPOSITORY} --body ${SQL_ADMIN_GROUP}"
  echo "gh variable set SQL_ADMIN_GROUP_OBJECT_ID --repo ${REPOSITORY} --body ${group_id}"
  echo "gh variable set BUDGET_EMAIL --repo ${REPOSITORY} --body <email>"
  echo "gh variable set ALERT_EMAIL --repo ${REPOSITORY} --body <email>"
  echo "gh variable set CUSTOM_DOMAIN_BINDING --repo ${REPOSITORY} --body None"
  echo "gh variable set DEPLOY_ENABLED --repo ${REPOSITORY} --body true"
  echo "openssl rand -hex 32 | gh secret set ESCALATION_API_KEY --repo ${REPOSITORY}"
  echo "gh secret set NOTIFICATION_WEBHOOK_URL --repo ${REPOSITORY}"
  echo "gh secret set CLOUDFLARE_API_TOKEN --repo ${REPOSITORY}"
  echo
  echo "# Optional: keep the environment on through a date (UTC, inclusive) so the daily power down skips"
  echo "gh variable set KEEP_ON_UNTIL --repo ${REPOSITORY} --body <yyyy-mm-dd>"
  echo
  echo "# After the first infrastructure deployment"
  echo "az staticwebapp secrets list --name stapp-incident-ops --resource-group ${RESOURCE_GROUP} --query properties.apiKey --output tsv | gh secret set SWA_DEPLOYMENT_TOKEN --repo ${REPOSITORY}"
}

main() {
  if [[ "${1:-}" == "--print-rbac-condition" ]]; then
    rbac_condition
    echo
    return
  fi
  readonly SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:?set AZURE_SUBSCRIPTION_ID}"
  command -v az > /dev/null 2>&1 || { echo "az CLI is required" >&2; exit 69; }
  command -v jq > /dev/null 2>&1 || { echo "jq is required" >&2; exit 69; }
  command -v curl > /dev/null 2>&1 || { echo "curl is required" >&2; exit 69; }

  az account set --subscription "$SUBSCRIPTION_ID"
  local tenant_id scope app_id object_id group_id
  tenant_id=$(az account show --query tenantId --output tsv)
  scope=$(ensure_resource_group)
  app_id=$(ensure_app)
  object_id=$(ensure_service_principal "$app_id")
  ensure_federated_credentials "$app_id"
  ensure_role "$object_id" "$CONTRIBUTOR_ROLE_ID" "$scope"
  ensure_role "$object_id" "$RBAC_ADMIN_ROLE_ID" "$scope" "$(rbac_condition)"
  group_id=$(ensure_sql_admin_group "$object_id")
  print_github_commands "$app_id" "$tenant_id" "$group_id"
}

main "$@"
