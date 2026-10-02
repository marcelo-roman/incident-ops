#!/usr/bin/env bash
set -euo pipefail

readonly RESOURCE_GROUP="${RESOURCE_GROUP:-rg-incident-ops}"
readonly DEPLOYMENT_NAME="${DEPLOYMENT_NAME:-incident-ops}"
readonly PRINCIPAL_NAME="${PRINCIPAL_NAME:-ca-incident-ops-api}"
readonly SQLCMD="${SQLCMD:-sqlcmd}"

command -v "$SQLCMD" > /dev/null 2>&1 || {
  echo "go-sqlcmd is required: https://github.com/microsoft/go-sqlcmd" >&2
  exit 69
}

server=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.endpoints.value.sqlServer" \
  --output tsv)
database=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.endpoints.value.sqlDatabase" \
  --output tsv)

if [[ -z "$server" || -z "$database" ]]; then
  echo "deployment ${DEPLOYMENT_NAME} has no SQL outputs in ${RESOURCE_GROUP}" >&2
  exit 1
fi

query=$(cat << SQL
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'${PRINCIPAL_NAME}')
    CREATE USER [${PRINCIPAL_NAME}] FROM EXTERNAL PROVIDER;
IF IS_ROLEMEMBER('db_datareader', '${PRINCIPAL_NAME}') = 0
    ALTER ROLE db_datareader ADD MEMBER [${PRINCIPAL_NAME}];
IF IS_ROLEMEMBER('db_datawriter', '${PRINCIPAL_NAME}') = 0
    ALTER ROLE db_datawriter ADD MEMBER [${PRINCIPAL_NAME}];
IF IS_ROLEMEMBER('db_ddladmin', '${PRINCIPAL_NAME}') = 0
    ALTER ROLE db_ddladmin ADD MEMBER [${PRINCIPAL_NAME}];
SQL
)

"$SQLCMD" \
  -S "$server" \
  -d "$database" \
  --authentication-method ActiveDirectoryDefault \
  -b \
  -Q "$query"

echo "granted ${PRINCIPAL_NAME} reader, writer and ddladmin on ${database}"
