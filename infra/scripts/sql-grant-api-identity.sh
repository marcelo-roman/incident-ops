#!/usr/bin/env bash
set -euo pipefail

readonly RESOURCE_GROUP="${RESOURCE_GROUP:-rg-incident-ops}"
readonly DEPLOYMENT_NAME="${DEPLOYMENT_NAME:-incident-ops}"
readonly PRINCIPAL_NAME="${PRINCIPAL_NAME:-ca-incident-ops-api}"
readonly SQLCMD="${SQLCMD:-sqlcmd}"
readonly SQL_AUTHENTICATION_METHOD="${SQL_AUTHENTICATION_METHOD:-ActiveDirectoryDefault}"
readonly FIREWALL_RULE_NAME="${FIREWALL_RULE_NAME:-allow-sql-grant-caller}"
readonly ATTEMPTS="${GRANT_ATTEMPTS:-10}"
readonly INTERVAL_SECONDS="${GRANT_INTERVAL_SECONDS:-15}"
readonly IDENTITY_API_VERSION="2023-01-31"
readonly GUID_PATTERN='^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'

firewall_server=""

log() {
  echo "==> $*" >&2
}

deployment_output() {
  az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.endpoints.value.$1" \
    --output tsv
}

identity_client_id() {
  local app_id
  app_id=$(az containerapp show --resource-group "$RESOURCE_GROUP" --name "$PRINCIPAL_NAME" --query id --output tsv)
  az rest \
    --method get \
    --url "${app_id}/providers/Microsoft.ManagedIdentity/identities/default?api-version=${IDENTITY_API_VERSION}" \
    --query properties.clientId \
    --output tsv
}

grant_statement() {
  local client_id="$1"
  cat << SQL
DECLARE @sid nvarchar(64) = CONVERT(nvarchar(64), CONVERT(varbinary(16), CONVERT(uniqueidentifier, N'${client_id}')), 1);
DECLARE @create nvarchar(max) = N'CREATE USER ' + QUOTENAME(N'${PRINCIPAL_NAME}') + N' WITH SID = ' + @sid + N', TYPE = E;';
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'${PRINCIPAL_NAME}')
    EXEC (@create);
IF IS_ROLEMEMBER('db_datareader', '${PRINCIPAL_NAME}') = 0
    ALTER ROLE db_datareader ADD MEMBER [${PRINCIPAL_NAME}];
IF IS_ROLEMEMBER('db_datawriter', '${PRINCIPAL_NAME}') = 0
    ALTER ROLE db_datawriter ADD MEMBER [${PRINCIPAL_NAME}];
IF IS_ROLEMEMBER('db_ddladmin', '${PRINCIPAL_NAME}') = 0
    ALTER ROLE db_ddladmin ADD MEMBER [${PRINCIPAL_NAME}];
SQL
}

open_firewall() {
  local caller_ip
  caller_ip=$(curl --silent --show-error --fail --max-time 10 https://api.ipify.org || true)
  if [[ -z "$caller_ip" ]]; then
    log "public IP unknown; relying on the existing firewall rules"
    return
  fi
  log "temporary firewall rule ${FIREWALL_RULE_NAME} for ${caller_ip}"
  az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$firewall_server" \
    --name "$FIREWALL_RULE_NAME" \
    --start-ip-address "$caller_ip" \
    --end-ip-address "$caller_ip" \
    --output none
}

close_firewall() {
  if [[ -z "$firewall_server" ]]; then
    return
  fi
  az sql server firewall-rule delete \
    --resource-group "$RESOURCE_GROUP" \
    --server "$firewall_server" \
    --name "$FIREWALL_RULE_NAME" \
    --output none 2> /dev/null || true
}

run_grant() {
  local server="$1"
  local database="$2"
  local statement="$3"
  local attempt
  for attempt in $(seq 1 "$ATTEMPTS"); do
    if "$SQLCMD" -S "$server" -d "$database" --authentication-method "$SQL_AUTHENTICATION_METHOD" -b -Q "$statement"; then
      return 0
    fi
    log "attempt ${attempt}/${ATTEMPTS} failed; retrying in ${INTERVAL_SECONDS}s"
    sleep "$INTERVAL_SECONDS"
  done
  return 1
}

main() {
  command -v "$SQLCMD" > /dev/null 2>&1 || {
    echo "go-sqlcmd is required: https://github.com/microsoft/go-sqlcmd" >&2
    exit 69
  }

  local server database client_id
  server=$(deployment_output sqlServer)
  database=$(deployment_output sqlDatabase)
  if [[ -z "$server" || -z "$database" ]]; then
    echo "deployment ${DEPLOYMENT_NAME} has no SQL outputs in ${RESOURCE_GROUP}" >&2
    exit 1
  fi

  client_id=$(identity_client_id)
  if [[ ! "$client_id" =~ $GUID_PATTERN ]]; then
    echo "could not resolve the client id of ${PRINCIPAL_NAME}" >&2
    exit 1
  fi

  firewall_server="${server%%.*}"
  trap close_firewall EXIT
  open_firewall

  log "granting ${PRINCIPAL_NAME} (${client_id}) on ${database}"
  run_grant "$server" "$database" "$(grant_statement "$client_id")" || {
    echo "could not grant ${PRINCIPAL_NAME} on ${database}" >&2
    exit 1
  }
  echo "granted ${PRINCIPAL_NAME} reader, writer and ddladmin on ${database}"
}

main "$@"
