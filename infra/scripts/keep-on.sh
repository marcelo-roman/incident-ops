#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
  echo "usage: $0 <keep-on-until yyyy-mm-dd or empty> [today yyyy-mm-dd]" >&2
  exit 64
fi

readonly KEEP_ON_UNTIL="$1"
readonly TODAY="${2:-$(date -u +%F)}"
readonly DATE_PATTERN='^[0-9]{4}-[0-9]{2}-[0-9]{2}$'

if [[ -z "$KEEP_ON_UNTIL" ]]; then
  echo "false"
  exit 0
fi

if [[ ! "$KEEP_ON_UNTIL" =~ $DATE_PATTERN ]]; then
  echo "KEEP_ON_UNTIL '${KEEP_ON_UNTIL}' is not yyyy-mm-dd; ignoring it" >&2
  echo "false"
  exit 0
fi

if [[ "$TODAY" > "$KEEP_ON_UNTIL" ]]; then
  echo "false"
  exit 0
fi

echo "true"
