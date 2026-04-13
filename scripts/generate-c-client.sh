#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SPEC="$ROOT/openapi/monitorcontrol.openapi.json"
OUT="$ROOT/generated/openapi-c"

if [[ ! -f "$SPEC" ]]; then
	echo "Missing $SPEC — run: bash scripts/fetch-openapi.sh" >&2
	exit 1
fi

mkdir -p "$ROOT/generated"

IMG="${OPENAPI_GENERATOR_IMAGE:-openapitools/openapi-generator-cli:v7.7.0}"

docker run --rm \
	-v "$ROOT:/local" \
	"$IMG" generate \
	-i /local/openapi/monitorcontrol.openapi.json \
	-g c \
	-o /local/generated/openapi-c \
	--additional-properties=discardUnknownProperties=true

echo "Generated $OUT"
