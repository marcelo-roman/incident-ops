#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
readonly BICEP_DIR="${SCRIPT_DIR}/../bicep"
readonly TEMPLATE_FILE="${BICEP_DIR}/main.bicep"
readonly PARAMETERS_FILE="${BICEP_DIR}/main.bicepparam"
readonly RESOURCE_GROUP="${RESOURCE_GROUP:-rg-incident-ops}"
readonly DEPLOYMENT_NAME="${DEPLOYMENT_NAME:-incident-ops}"
readonly API_APP="ca-incident-ops-api"
readonly INSIGHTS_APP="ca-incident-ops-insights"
readonly STATIC_WEB_APP="stapp-incident-ops"
readonly SQL_DATABASE="sqldb-incident-ops"
readonly DOCS_URL="https://incidents-docs.marceloroman.com.br"
readonly READY_ATTEMPTS="${READY_ATTEMPTS:-40}"
readonly READY_INTERVAL_SECONDS="${READY_INTERVAL_SECONDS:-15}"
readonly REQUIRED_DEPLOY_VARIABLES=(
  ESCALATION_API_KEY
  SQL_ADMIN_GROUP_NAME
  SQL_ADMIN_GROUP_OBJECT_ID
  BUDGET_EMAIL
  CUSTOM_DOMAIN_BINDING
)

log() {
  echo "==> $*" >&2
}

usage() {
  echo "usage: $0 up|down|status" >&2
  exit 64
}

require_command() {
  command -v "$1" > /dev/null 2>&1 && return
  echo "$1 is required" >&2
  exit 69
}

require_deploy_variables() {
  local name missing=()
  for name in "${REQUIRED_DEPLOY_VARIABLES[@]}"; do
    if [[ -z "${!name:-}" ]]; then
      missing+=("$name")
    fi
  done
  if [[ ${#missing[@]} -eq 0 ]]; then
    return
  fi
  echo "set ${missing[*]} with the values the infra workflow uses" >&2
  exit 64
}

ensure_containerapp_extension() {
  if az extension show --name containerapp > /dev/null 2>&1; then
    return
  fi
  az extension add --name containerapp --only-show-errors
}

first_resource_name() {
  az resource list \
    --resource-group "$RESOURCE_GROUP" \
    --resource-type "$1" \
    --query "[0].name" \
    --output tsv
}

sql_server_name() {
  first_resource_name Microsoft.Sql/servers
}

service_bus_namespace() {
  first_resource_name Microsoft.ServiceBus/namespaces
}

sql_database_sku() {
  local server="$1"
  if [[ -z "$server" ]]; then
    return
  fi
  az sql db list \
    --resource-group "$RESOURCE_GROUP" \
    --server "$server" \
    --query "[?name=='${SQL_DATABASE}'].currentSku.name | [0]" \
    --output tsv
}

dead_letter_alert_ids() {
  az resource list \
    --resource-group "$RESOURCE_GROUP" \
    --resource-type Microsoft.Insights/metricAlerts \
    --query "[?ends_with(name, '-dead-letters')].id" \
    --output tsv
}

resolve_images() {
  if [[ -z "${API_IMAGE:-}" ]]; then
    API_IMAGE=$("${SCRIPT_DIR}/resolve-image.sh" "$RESOURCE_GROUP" "$API_APP" ghcr.io/marcelo-roman/incident-ops-api:latest)
    export API_IMAGE
  fi
  if [[ -z "${INSIGHTS_IMAGE:-}" ]]; then
    INSIGHTS_IMAGE=$("${SCRIPT_DIR}/resolve-image.sh" "$RESOURCE_GROUP" "$INSIGHTS_APP" ghcr.io/marcelo-roman/incident-ops-insights:latest)
    export INSIGHTS_IMAGE
  fi
}

deploy() {
  local state="$1"
  log "deploying ${DEPLOYMENT_NAME} with environmentState=${state}"
  ENVIRONMENT_STATE="$state" az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --template-file "$TEMPLATE_FILE" \
    --parameters "$PARAMETERS_FILE" \
    --output none
}

grant_api_identity() {
  log "granting the API identity on ${SQL_DATABASE}"
  env RESOURCE_GROUP="$RESOURCE_GROUP" DEPLOYMENT_NAME="$DEPLOYMENT_NAME" PRINCIPAL_NAME="$API_APP" \
    "${SCRIPT_DIR}/sql-grant-api-identity.sh"
}

restart_api() {
  local revision
  revision=$(az containerapp show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$API_APP" \
    --query properties.latestRevisionName \
    --output tsv)
  log "restarting ${revision}"
  az containerapp revision restart \
    --resource-group "$RESOURCE_GROUP" \
    --name "$API_APP" \
    --revision "$revision" \
    --output none
}

app_base_url() {
  local app="$1"
  local custom_host fqdn
  custom_host=$(az containerapp show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$app" \
    --query "properties.configuration.ingress.customDomains[?bindingType=='SniEnabled'].name | [0]" \
    --output tsv)
  if [[ -n "$custom_host" ]]; then
    echo "https://${custom_host}"
    return
  fi
  fqdn=$(az containerapp show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$app" \
    --query properties.configuration.ingress.fqdn \
    --output tsv)
  echo "https://${fqdn}"
}

web_base_url() {
  local custom_host default_host
  custom_host=$(az staticwebapp hostname list \
    --resource-group "$RESOURCE_GROUP" \
    --name "$STATIC_WEB_APP" \
    --query "[?status=='Ready'].domainName | [0]" \
    --output tsv 2> /dev/null || true)
  if [[ -n "$custom_host" ]]; then
    echo "https://${custom_host}"
    return
  fi
  default_host=$(az staticwebapp show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$STATIC_WEB_APP" \
    --query defaultHostname \
    --output tsv)
  echo "https://${default_host}"
}

wait_until_ready() {
  local url="$1"
  local attempt status
  for attempt in $(seq 1 "$READY_ATTEMPTS"); do
    status=$(curl --silent --output /dev/null --write-out "%{http_code}" --max-time 20 "$url" || echo "000")
    if [[ "$status" =~ ^2 ]]; then
      log "${url} answered ${status}"
      return 0
    fi
    log "attempt ${attempt}/${READY_ATTEMPTS}: ${url} answered ${status}"
    sleep "$READY_INTERVAL_SECONDS"
  done
  echo "${url} did not become ready" >&2
  return 1
}

delete_service_bus() {
  local namespace
  namespace=$(service_bus_namespace)
  if [[ -z "$namespace" ]]; then
    log "no Service Bus namespace"
    return
  fi
  log "deleting Service Bus namespace ${namespace}"
  az servicebus namespace delete --resource-group "$RESOURCE_GROUP" --name "$namespace" --output none
}

delete_dead_letter_alerts() {
  local alert_id
  while IFS= read -r alert_id; do
    if [[ -z "$alert_id" ]]; then
      continue
    fi
    log "deleting ${alert_id##*/}"
    az resource delete --ids "$alert_id" --output none
  done < <(dead_letter_alert_ids)
}

delete_sql_database() {
  local server
  server=$(sql_server_name)
  if [[ -z "$(sql_database_sku "$server")" ]]; then
    log "no SQL database"
    return
  fi
  log "deleting SQL database ${SQL_DATABASE} on ${server}"
  az sql db delete --resource-group "$RESOURCE_GROUP" --server "$server" --name "$SQL_DATABASE" --yes --output none
}

print_urls() {
  local api_url
  api_url=$(app_base_url "$API_APP")
  echo
  echo "Console   $(web_base_url)"
  echo "API       ${api_url}"
  echo "Swagger   ${api_url}/swagger"
  echo "Insights  $(app_base_url "$INSIGHTS_APP")"
  echo "Docs      ${DOCS_URL}"
}

up() {
  require_command "${SQLCMD:-sqlcmd}"
  require_command curl
  require_deploy_variables
  ensure_containerapp_extension
  resolve_images
  deploy on
  grant_api_identity
  restart_api
  wait_until_ready "$(app_base_url "$API_APP")/health/ready"
  print_urls
}

down() {
  require_deploy_variables
  ensure_containerapp_extension
  resolve_images
  deploy off
  delete_dead_letter_alerts
  delete_service_bus
  delete_sql_database
  status
}

status() {
  local namespace server database_sku min_replicas state
  ensure_containerapp_extension
  namespace=$(service_bus_namespace)
  server=$(sql_server_name)
  database_sku=$(sql_database_sku "$server")
  min_replicas=$(az containerapp show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$API_APP" \
    --query properties.template.scale.minReplicas \
    --output tsv 2> /dev/null || true)
  state="partial"
  if [[ -n "$namespace" && -n "$database_sku" ]]; then
    state="on"
  fi
  if [[ -z "$namespace" && -z "$database_sku" ]]; then
    state="off"
  fi
  echo "state                  ${state}"
  echo "Service Bus namespace  ${namespace:-absent}"
  echo "SQL database           ${database_sku:+${SQL_DATABASE} (${database_sku})}${database_sku:-absent}"
  echo "API minimum replicas   ${min_replicas:-unknown}"
  if [[ "$state" == "partial" ]]; then
    echo "run '$0 up' or '$0 down' to converge" >&2
  fi
}

main() {
  if [[ $# -ne 1 ]]; then
    usage
  fi
  require_command az
  case "$1" in
    up) up ;;
    down) down ;;
    status) status ;;
    *) usage ;;
  esac
}

main "$@"
