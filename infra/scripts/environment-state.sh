#!/usr/bin/env bash
set -euo pipefail

if [[ $# -gt 1 ]]; then
  echo "usage: $0 [resource-group]" >&2
  exit 64
fi

readonly RESOURCE_GROUP="${1:-rg-incident-ops}"

namespaces=$(az resource list \
  --resource-group "$RESOURCE_GROUP" \
  --resource-type Microsoft.ServiceBus/namespaces \
  --query "length(@)" \
  --output tsv)

if [[ "$namespaces" == "0" ]]; then
  echo "off"
  exit 0
fi

echo "on"
