# VMC string catalog

This spec page is the **entry point** for all VMC ASCII documentation in this repository.

| Document | Purpose |
|----------|---------|
| [reference/vmc-command-surface.md](../reference/vmc-command-surface.md) | Categories, SDK usage, human token table, links to parity + firmware warnings |
| [reference/appendices/vmc-stat-tokens-from-references.txt](../reference/appendices/vmc-stat-tokens-from-references.txt) | **Exhaustive** `STATget` / `STATset` / broadcast literals from the `references/**/*.cs` corpus (regenerated) |
| [reference/references-parity.md](../reference/references-parity.md) | Every major `references/` subtree mapped to `src/` types; **not ported** items; VMC gaps (`sendCommandBroadCast`, empty `STATset`, `STATret`, `MODEL2`) |

SDK entry: [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs).  
HTTP: `POST /api/vmc/get` and `POST /api/vmc/set` in [guide/web-api-and-python-gateway.md](../guide/web-api-and-python-gateway.md).

**ESP32 native (on-wire, no HTTP):** [`examples/esp32-sdcp-vmc/monitor_knobs_sdcp.ino`](../../examples/esp32-sdcp-vmc/monitor_knobs_sdcp.ino) builds the same ASCII tails inside SDCP v3 item **0xB000** frames — diagram vs C# in [diagrams/monitor-control-flows.md](../diagrams/monitor-control-flows.md).
