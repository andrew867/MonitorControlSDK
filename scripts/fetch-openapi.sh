#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$ROOT/openapi"
PORT="${PORT:-55055}"
URL="http://127.0.0.1:${PORT}"

dotnet run --project "$ROOT/src/MonitorControl.Web" --urls "${URL}" &
PID=$!

cleanup() {
	kill "$PID" 2>/dev/null || true
	wait "$PID" 2>/dev/null || true
}
trap cleanup EXIT

for _ in $(seq 1 80); do
	if curl -sf "${URL}/swagger/v1/swagger.json" -o "$ROOT/openapi/monitorcontrol.openapi.json"; then
		echo "Wrote $ROOT/openapi/monitorcontrol.openapi.json"
		exit 0
	fi
	sleep 0.25
done

echo "Timeout: could not download ${URL}/swagger/v1/swagger.json" >&2
exit 1
