#!/usr/bin/env bash
# Regenerate machine-owned appendices under docs/reference/appendices/
# Run from repository root:  bash scripts/regen-appendices.sh

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP="$ROOT/docs/reference/appendices"
SDK="$ROOT/src/MonitorControlSDK"
REF="$ROOT/references"

mkdir -p "$APP"

echo "== VMS opcode constants (from shipped SDK) =="
rg "private const byte CMD_" "$SDK/Internal/LegacyVmsContainer.cs" >"$APP/vms-opcode-constants.txt"

echo "== VMS engine send methods =="
rg "^\tpublic int send" "$SDK/Protocol/VmsCommandEngine.cs" >"$APP/vms-engine-send-methods.txt"

echo "== VMC STAT tokens from references/ (C# only) =="
{
	echo "# VMC STAT tokens collected from references/ (C# corpus)"
	echo "#"
	echo "# Regenerated: $(date -u +%Y-%m-%dT%H:%MZ) — bash scripts/regen-appendices.sh"
	echo "# Scanned: references/**/*.cs"
	echo "#"
	echo "# Encoding: VmcClient.Send(\"STATset\", seg1, seg2, ...) joins with spaces (LegacyVmcContainer.setCommand)."
	echo "# Some forks used one string \"FLATFIELDPATTERN OFF\" vs two args \"FLATFIELDPATTERN\", \"OFF\" — same wire."
	echo "#"
	echo "# --- STATget / getSTATgetMessage string literals ---"
	(
		rg -o 'STATget", "[^"]+' "$REF" --glob '*.cs' 2>/dev/null | sed 's/.*STATget", "//'
		rg -o 'getSTATgetMessage\("[^"]+' "$REF" --glob '*.cs' 2>/dev/null | sed 's/.*getSTATgetMessage("//'
	) | sort -u

	echo ""
	echo "# --- STATset first literal (sendCommand second argument); more args may follow ---"
	rg -o 'STATset", "[^"]+' "$REF" --glob '*.cs' 2>/dev/null | sed 's/.*STATset", "//' | sort -u

	echo ""
	echo "# --- sendCommandBroadCast(\"STATset\", \"...\") — original UDP/broadcast helper; not in Sony.MonitorControl TCP stack ---"
	rg -o 'sendCommandBroadCast\("STATset", "[^"]+' "$REF" --glob '*.cs' 2>/dev/null | sed 's/.*", "//' | sort -u

	echo ""
	echo "# --- Notes ---"
	echo "# STATret appears as VMC_CMM_RET constant in several ControlVmcCommand files; no sendCommand(\"STATret\"...) literal found in references/**/*.cs scan."
	echo "# sendCommand(\"STATset\", \"\", ...) appears in historical firmware-updater sources (empty tail)."
} >"$APP/vmc-stat-tokens-from-references.txt"

echo "Wrote:"
echo "  $APP/vms-opcode-constants.txt"
echo "  $APP/vms-engine-send-methods.txt"
echo "  $APP/vmc-stat-tokens-from-references.txt"
