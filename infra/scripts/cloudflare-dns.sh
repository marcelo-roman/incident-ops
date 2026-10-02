#!/usr/bin/env bash
set -euo pipefail

readonly API="https://api.cloudflare.com/client/v4"
readonly ZONE_NAME="${CLOUDFLARE_ZONE_NAME:-marceloroman.com.br}"

usage() {
  echo "usage: CLOUDFLARE_API_TOKEN=... $0 <dns-records.json>" >&2
  echo "dns-records.json is the dnsRecords output of the bicep deployment: [{\"type\",\"name\",\"content\"}]" >&2
  exit 64
}

require() {
  command -v "$1" > /dev/null 2>&1 || {
    echo "missing dependency: $1" >&2
    exit 69
  }
}

cloudflare() {
  local method="$1"
  local path="$2"
  local body="${3:-}"
  local response
  if [[ -z "$body" ]]; then
    response=$(curl -sS -X "$method" "${API}${path}" \
      -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
      -H "Content-Type: application/json")
  else
    response=$(curl -sS -X "$method" "${API}${path}" \
      -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
      -H "Content-Type: application/json" \
      --data "$body")
  fi
  if [[ "$(jq -r '.success' <<< "$response")" != "true" ]]; then
    echo "cloudflare ${method} ${path} failed: $(jq -c '.errors' <<< "$response")" >&2
    exit 1
  fi
  echo "$response"
}

zone_id() {
  local id
  id=$(cloudflare GET "/zones?name=${ZONE_NAME}" | jq -r '.result[0].id // empty')
  if [[ -z "$id" ]]; then
    echo "zone not found: ${ZONE_NAME}" >&2
    exit 1
  fi
  echo "$id"
}

record_body() {
  local type="$1"
  local name="$2"
  local content="$3"
  if [[ "$type" == "CNAME" ]]; then
    jq -nc --arg type "$type" --arg name "$name" --arg content "$content" \
      '{type: $type, name: $name, content: $content, ttl: 1, proxied: false, comment: "incident-ops"}'
    return
  fi
  jq -nc --arg type "$type" --arg name "$name" --arg content "$content" \
    '{type: $type, name: $name, content: $content, ttl: 1, comment: "incident-ops"}'
}

normalize() {
  local value="${1%.}"
  value="${value#\"}"
  echo "${value%\"}"
}

upsert() {
  local zone="$1"
  local type="$2"
  local name="$3"
  local content="$4"
  local existing record_id current
  existing=$(cloudflare GET "/zones/${zone}/dns_records?type=${type}&name=${name}")
  record_id=$(jq -r '.result[0].id // empty' <<< "$existing")
  local body
  body=$(record_body "$type" "$name" "$content")

  if [[ -z "$record_id" ]]; then
    cloudflare POST "/zones/${zone}/dns_records" "$body" > /dev/null
    echo "created ${type} ${name} -> ${content}"
    return
  fi

  current=$(jq -r '.result[0].content' <<< "$existing")
  local proxied
  proxied=$(jq -r '.result[0].proxied // false' <<< "$existing")
  if [[ "$(normalize "$current")" == "$(normalize "$content")" && "$proxied" == "false" ]]; then
    echo "unchanged ${type} ${name}"
    return
  fi

  cloudflare PUT "/zones/${zone}/dns_records/${record_id}" "$body" > /dev/null
  echo "updated ${type} ${name} -> ${content}"
}

main() {
  [[ $# -eq 1 ]] || usage
  [[ -n "${CLOUDFLARE_API_TOKEN:-}" ]] || {
    echo "CLOUDFLARE_API_TOKEN is not set" >&2
    exit 64
  }
  [[ -f "$1" ]] || {
    echo "file not found: $1" >&2
    exit 66
  }
  require curl
  require jq

  local zone
  zone=$(zone_id)

  local count index type name content
  count=$(jq 'length' "$1")
  for ((index = 0; index < count; index++)); do
    type=$(jq -r ".[${index}].type" "$1")
    name=$(jq -r ".[${index}].name" "$1")
    content=$(jq -r ".[${index}].content" "$1")
    if [[ "$name" != *"${ZONE_NAME}" ]]; then
      echo "skipping ${name}: outside zone ${ZONE_NAME}" >&2
      continue
    fi
    upsert "$zone" "$type" "$name" "$content"
  done
}

main "$@"
