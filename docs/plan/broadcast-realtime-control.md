# Plan: Broadcast real-time control sample

## Goal

Ship [samples/Sample.BroadcastControl](../../samples/Sample.BroadcastControl): an operator-facing console that stays connected to a monitor over SDCP and applies **immediate** VMC (and optional VMS) changes while the display is in use (e.g. live grading / broadcast shading), without restarting the tool.

## Scope

- **In scope:** TCP SDCP session, interactive command loop, `STATget` / `STATset` for common shading fields, graceful shutdown, documented risks (wrong values on air).
- **Out of scope for v1:** Firmware transfer, VMA upgrade paths, multi-monitor fan-out (single `--host` only).

## Dependencies

- `MonitorControl` library: `SdcpConnection`, `VmcClient`, optional `VmsClient` for future extension.

## Test plan

See [docs/testing/broadcast-realtime-control-tests.md](../testing/broadcast-realtime-control-tests.md).

## Milestones

1. Spec + test plan documents (this repo).
2. Optional `StreamSdcpTransport` in SDK for loopback protocol tests (no TCP mock for framing).
3. Sample project + README in `samples/Sample.BroadcastControl/`.
4. CI builds sample on Windows/Linux/macOS.

## Success criteria

- Operator can connect once and issue repeated `set` / `get` commands with sub-second handling (network permitting).
- `dotnet run --project samples/Sample.BroadcastControl -- --host …` documented.
- Tests cover command parsing and stream transport framing where feasible without hardware.
