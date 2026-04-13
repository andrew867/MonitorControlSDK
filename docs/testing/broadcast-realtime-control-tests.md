# Test plan: Broadcast real-time control

## Automated (CI)

| Test | Location | Purpose |
|------|----------|---------|
| `BroadcastCommandLineTests` | `tests/MonitorControlSDK.Tests/BroadcastCommandLineTests.cs` | Parse `get`/`set` lines into VMC call shapes (no network). |
| `StreamSdcpTransportTests` | `tests/MonitorControlSDK.Tests/StreamSdcpTransportTests.cs` | Pre-seeded `MemoryStream`: send V3 packet, receive fixed-length buffer, assert bytes round-trip. |

## Manual (hardware)

1. Connect monitor on isolated VLAN; note IP.
2. `dotnet run --project samples/Sample.BroadcastControl -- --host <ip>`
3. `get MODEL` — expect non-empty string.
4. `set BRIGHTNESS <value>` then `get BRIGHTNESS` — expect consistency with device UI (model-dependent).
5. `quit` — process exits 0; monitor still responsive.

## Negative cases

- Invalid host: connection error message, non-zero exit.
- Malformed line: print error, continue loop (do not crash).

## Not tested in CI

End-to-end shading correctness (probe / meter required).
