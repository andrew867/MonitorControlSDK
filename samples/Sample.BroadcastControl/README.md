# Sample: broadcast real-time control

Interactive **REPL** over **one long-lived** SDCP **TCP** session (`SdcpConnection` port **53484**). Use it for repeated `STATget` / `STATset` while grading or shading without reconnecting per command.

**Specification** (grammar, safety, transport): [docs/spec/broadcast-realtime-control.md](../../docs/spec/broadcast-realtime-control.md).

**Tests** (parser + stream transport): [docs/testing/broadcast-realtime-control-tests.md](../../docs/testing/broadcast-realtime-control-tests.md).

**Diagram** (REPL vs one-shot HTTP): [docs/diagrams/monitor-control-flows.md](../../docs/diagrams/monitor-control-flows.md).

## Run

Host is the **first non-option argument** or follows `--host`:

```bash
dotnet run --project samples/Sample.BroadcastControl -- 192.168.0.10
dotnet run --project samples/Sample.BroadcastControl -- --host 192.168.0.10
dotnet run --project samples/Sample.BroadcastControl -- --vmc-item B001 --sdcp-unit 1 --host 192.168.0.10
```

Optional flags (see [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs)):

- **`--sdcp-unit <0–255>`** — SDCP single-connection unit (group 0); omit for P2P `(0,0)`.
- **`--vmc-item`** — `B000`, `monitor` (default), or `B001`, `builtIn`, `built_in`, `builtin` for item **`B001h`**.

## Commands (summary)

| Input | Effect |
|-------|--------|
| `get <field>` | `STATget <field>` — prints value or `(null)`. |
| `set <token> [args...]` | `STATset` with tail tokens (e.g. `set BRIGHTNESS 512`, `set FLATFIELDPATTERN OFF`). |
| `help` | Lists commands. |
| `quit` / `exit` | Closes TCP and exits **0**. |

**Not in this sample:** UDP **Group / All** broadcast (`VmcUdpBroadcastClient`, `vmc-broadcast`, `POST /api/vmc/broadcast`) — see [spec/broadcast-realtime-control.md](../../docs/spec/broadcast-realtime-control.md) §Transport.

## Related samples

- **One-shot VMC get:** [`Sample.Vmc`](../Sample.Vmc/).
- **UDP multi-monitor shading:** [`Sample.UdpVmcBroadcast`](../Sample.UdpVmcBroadcast/).
- **ESP32 on-wire knobs (no .NET on device):** [`examples/esp32-sdcp-vmc`](../../examples/esp32-sdcp-vmc/).
