# Test plan: Broadcast real-time control

Spec for the sample: [spec/broadcast-realtime-control.md](../spec/broadcast-realtime-control.md).

## Automated (CI)

| Test class | Source file | Purpose |
|------------|-------------|---------|
| `BroadcastControlLineParserTests` | [`tests/MonitorControlSDK.Tests/BroadcastControlLineParserTests.cs`](../../tests/MonitorControlSDK.Tests/BroadcastControlLineParserTests.cs) | Parse `get` / `set` REPL lines into VMC call shapes (no network). |
| `StreamSdcpTransportTests` | [`tests/MonitorControlSDK.Tests/StreamSdcpTransportTests.cs`](../../tests/MonitorControlSDK.Tests/StreamSdcpTransportTests.cs) | Pre-seeded `MemoryStream`: send V3 packet, receive fixed-length buffer, assert bytes round-trip. |

These run under `dotnet test MonitorControl.sln` in GitHub Actions / GitLab because the **sample project** is part of [`MonitorControl.sln`](../../MonitorControl.sln).

## Manual (hardware)

1. Connect monitor on isolated VLAN; note IP.
2. Start the REPL (either form is accepted — see [`Sample.BroadcastControl/Program.cs`](../../samples/Sample.BroadcastControl/Program.cs) `TryParseHost`):

   ```bash
   dotnet run --project samples/Sample.BroadcastControl -- 192.168.0.10
   dotnet run --project samples/Sample.BroadcastControl -- --host 192.168.0.10
   ```

3. `get MODEL` — expect non-empty string (or chassis-specific behavior documented in [vmc-command-surface.md](../reference/vmc-command-surface.md)).
4. `set BRIGHTNESS <value>` then `get BRIGHTNESS` — expect consistency with device UI (model-dependent).
5. `help` — lists commands.
6. `quit` — process exits 0; monitor still responsive.

## Negative cases

- Invalid host: connection error message, non-zero exit.
- Malformed line: print error, continue loop (do not crash).

## Not tested in CI

End-to-end shading correctness (probe / meter required). **ESP32 native** path ([`examples/esp32-sdcp-vmc`](../../examples/esp32-sdcp-vmc/)) is validated manually on hardware, not in automated CI.
