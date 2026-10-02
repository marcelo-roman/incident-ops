#!/usr/bin/env bash
set -euo pipefail

API="${API:-http://localhost:5080}"
ALERTMANAGER="${ALERTMANAGER:-http://localhost:9093}"
CONSOLE="${CONSOLE:-http://localhost:8080}"
ERROR_RATE="${ERROR_RATE:-0.5}"
FAULT_SECONDS="${FAULT_SECONDS:-420}"
WAIT_FOR_AUTO_MITIGATION="${WAIT_FOR_AUTO_MITIGATION:-true}"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
env_file="${script_dir}/../.env"
if [[ -z "${INCIDENT_OPS_API_KEY:-}" && -f "${env_file}" ]]; then
  INCIDENT_OPS_API_KEY="$(grep -E '^INCIDENT_OPS_API_KEY=' "${env_file}" | cut -d= -f2-)"
fi
API_KEY="${INCIDENT_OPS_API_KEY:-local-development-key}"

step() { printf '\n\033[1m==> %s\033[0m\n' "$*"; }
fail() { printf 'error: %s\n' "$*" >&2; exit 1; }

require() {
  command -v "$1" >/dev/null || fail "$1 is required"
}

wait_until() {
  local description="$1" timeout="$2"
  shift 2
  local deadline=$((SECONDS + timeout))
  until "$@"; do
    (( SECONDS < deadline )) || fail "timed out after ${timeout}s waiting for ${description}"
    sleep 5
  done
}

api_ready() { curl -fsS "${API}/health/ready" >/dev/null 2>&1; }

with_retries() {
  local attempt
  for attempt in 1 2 3 4 5 6 7 8 9 10; do
    if "$@" 2>/dev/null; then
      return 0
    fi
    sleep 1
  done
  fail "request kept failing after ${attempt} attempts: $*"
}

get() { with_retries curl -fsS "$@"; }

alert_firing() {
  curl -fsS "${ALERTMANAGER}/api/v2/alerts?active=true&filter=alertname%3D%22ApiHighErrorRate%22" \
    | jq -e 'length > 0' >/dev/null
}

find_alert_incident() {
  incident_json="$(get "${API}/api/incidents?open=true" \
    | jq -c '[.[] | select(.source == "Alertmanager")] | first // empty')"
  [[ -n "${incident_json}" ]]
}

incident_status() {
  get "${API}/api/incidents/${incident_id}" | jq -r '.status'
}

incident_mitigated() { [[ "$(incident_status)" == "Mitigated" ]]; }

generate_traffic() {
  local until_epoch=$(( $(date +%s) + FAULT_SECONDS ))
  while (( $(date +%s) < until_epoch )); do
    curl -s -o /dev/null "${API}/api/incidents?open=true" || true
    curl -s -o /dev/null "${API}/api/services" || true
    sleep 0.2
  done
}

post() {
  with_retries curl -fsS -X POST -H 'Content-Type: application/json' "$@"
}

require curl
require jq

step "Waiting for the API at ${API}"
wait_until "API readiness" 180 api_ready

step "Injecting fault: ${ERROR_RATE} error rate for ${FAULT_SECONDS}s"
post -H "X-Api-Key: ${API_KEY}" "${API}/api/chaos/faults" \
  -d "{\"errorRate\": ${ERROR_RATE}, \"latencyMs\": 0, \"durationSeconds\": ${FAULT_SECONDS}}" | jq .

step "Generating traffic so Prometheus sees the error rate"
generate_traffic &
traffic_pid=$!
trap 'kill ${traffic_pid} 2>/dev/null || true' EXIT

step "Waiting for ApiHighErrorRate to fire in Alertmanager (rule: > 5% for 2m, plus group_wait 30s)"
wait_until "ApiHighErrorRate" 360 alert_firing
echo "Alert firing: ${ALERTMANAGER}/#/alerts"

step "Waiting for the incident created from the alert"
incident_json=""
wait_until "alert incident" 120 find_alert_incident
incident_id="$(jq -r '.id' <<<"${incident_json}")"
jq '{number, title, severity, serviceId, status, source, escalationLevel, ackDueAt, slaState}' <<<"${incident_json}"
echo "Console: ${CONSOLE}"

step "Escalating as CheckAcknowledgementSla does at ackDueAt (Sev2 waits 30 minutes; doing it now)"
escalated_json="$(post -H "X-Api-Key: ${API_KEY}" "${API}/api/incidents/${incident_id}/escalate" \
  -d '{"reason": "Acknowledgement window missed (local demo)"}')"
jq '{number, status, escalationLevel, ackDueAt}' <<<"${escalated_json}"
level="$(jq -r '.escalationLevel' <<<"${escalated_json}")"
get "${API}/api/oncall/current" | jq --argjson level "${level}" '{escalatedTo: ([.primary, .secondary, .lead][$level - 1])}'

if [[ "${WAIT_FOR_AUTO_MITIGATION}" == "true" ]]; then
  step "Waiting for the fault to expire: the 5m rate window drains, Alertmanager sends resolved after group_interval (5m), the incident moves to Mitigated"
  wait_until "auto-mitigation" $((FAULT_SECONDS + 900)) incident_mitigated
  echo "Status: $(incident_status)"
fi

step "Resolving with a root cause"
post "${API}/api/incidents/${incident_id}/resolve" \
  -d '{"actor": "demo", "rootCause": "Injected fault: 50% of requests returned 500 via /api/chaos/faults."}' \
  | jq '{number, status, slaState, resolvedAt}'

step "Timeline"
get "${API}/api/incidents/${incident_id}" | jq -r '.timeline[] | "\(.at)  \(.kind)\t\(.actor)\t\(.message)"'
