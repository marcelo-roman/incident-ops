#!/bin/sh
set -eu

runtime_dir=/usr/share/nginx/runtime
api_origin="${API_BASE_URL%/}"
insights_origin="${INSIGHTS_BASE_URL%/}"

require_url() {
  case "$2" in
    http://* | https://*) ;;
    *)
      echo "$1 must be an absolute http(s) URL, got '$2'" >&2
      exit 1
      ;;
  esac
  if printf '%s' "$2" | grep -q "[[:space:]\"'\\;]"; then
    echo "$1 contains characters that are not valid in a base URL: '$2'" >&2
    exit 1
  fi
}

websocket_url() {
  printf '%s' "$1" | sed -e 's#^https://#wss://#' -e 's#^http://#ws://#'
}

require_url API_BASE_URL "$api_origin"
require_url INSIGHTS_BASE_URL "$insights_origin"

printf '{"apiBaseUrl":"%s","insightsBaseUrl":"%s"}\n' "$api_origin" "$insights_origin" >"$runtime_dir/config.json"

connect_src="'self' $api_origin $(websocket_url "$api_origin") $insights_origin https://*.service.signalr.net wss://*.service.signalr.net"

cat >"$runtime_dir/security-headers.conf" <<EOF
add_header Content-Security-Policy "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src $connect_src; worker-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
EOF

echo "Runtime configuration: API $api_origin, Insights $insights_origin"
