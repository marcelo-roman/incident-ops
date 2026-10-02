#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 3 ]]; then
  echo "usage: $0 <resource-group> <container-app-name> <fallback-image>" >&2
  exit 64
fi

readonly RESOURCE_GROUP="$1"
readonly APP_NAME="$2"
readonly FALLBACK_IMAGE="$3"

current=$(az containerapp show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --query "properties.template.containers[0].image" \
  --output tsv 2> /dev/null || true)

if [[ -z "$current" ]]; then
  echo "$FALLBACK_IMAGE"
  exit 0
fi

echo "$current"
