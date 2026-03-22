#!/usr/bin/env bash
# Run dotnet test under a wall-clock timeout; kill the process group when time expires.
# Default: DOTNET_TEST_TIMEOUT_SECONDS or 300 seconds. Maximum permitted: 300 seconds (5 minutes).
# Exit 124 on timeout (GNU timeout convention). On 124, optimize bottlenecks — do not disable tests to hide slowness.
#
# Usage:
#   ./scripts/dotnet-test.sh [extra timeout args] -- dotnet-test-args...
#   ./scripts/dotnet-test.sh KPatcher.sln -c Debug
#
# Requires GNU timeout (Linux: util-linux; macOS: brew install coreutils → use gtimeout by setting TIMEOUT_CMD).

set -euo pipefail

MAX_WRAPPER_SECS=300

resolve_timeout() {
  local v="${DOTNET_TEST_TIMEOUT_SECONDS:-$MAX_WRAPPER_SECS}"
  if [[ ! "$v" =~ ^[1-9][0-9]*$ ]]; then
    v="$MAX_WRAPPER_SECS"
  fi
  if [ "$v" -gt "$MAX_WRAPPER_SECS" ]; then
    echo "$MAX_WRAPPER_SECS"
  else
    echo "$v"
  fi
}

TIMEOUT_SECS="$(resolve_timeout)"

if [[ $# -eq 0 ]] && [[ -f KPatcher.sln ]]; then
  set -- KPatcher.sln -c Debug
fi

TIMEOUT_BIN="${TIMEOUT_CMD:-}"
if [[ -z "$TIMEOUT_BIN" ]]; then
  if command -v timeout >/dev/null 2>&1; then
    TIMEOUT_BIN="timeout"
  elif command -v gtimeout >/dev/null 2>&1; then
    TIMEOUT_BIN="gtimeout"
  else
    echo "dotnet-test.sh: need GNU 'timeout' or 'gtimeout' (coreutils). On macOS: brew install coreutils" >&2
    echo "Or set TIMEOUT_CMD to the full path of timeout." >&2
    exit 127
  fi
fi

# -k 10: if process does not exit after SIGTERM, SIGKILL 10s later
exec "$TIMEOUT_BIN" -k 10 "$TIMEOUT_SECS" dotnet test "$@"
