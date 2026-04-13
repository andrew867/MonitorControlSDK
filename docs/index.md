# MonitorControlSDK documentation

Single source of truth for **MonitorControl.Sdk** — SDAP discovery and SDCP control for compatible professional monitors. Everything below is grounded in this repository’s C# sources or cited public materials.

## New here? (~10 minutes)

1. Read [**Quick start**](quickstart.md) — install .NET 8, build, run discovery, send your first VMC query.
2. Skim [**SDCP framing and item numbers**](reference/sdcp-framing-and-items.md) — ports, headers, V3 vs V4.
3. Optional: [**Control flow diagrams (Mermaid)**](diagrams/monitor-control-flows.md) — discovery, SDCP, HTTP gateway, and knob-style controls.
4. Pick your path:
   - **ASCII shading / menus:** [**VMC command surface**](reference/vmc-command-surface.md) + [`VmcClient`](../src/MonitorControlSDK/Clients/VmcClient.cs) (TCP) + [`VmcUdpBroadcastClient`](../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs) (UDP Group/All).
   - **Structured factory / setup:** [**VMS overview**](reference/vms-overview.md) + [`VmsCommandEngine`](../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs).
   - **Service / firmware (dangerous):** [**VMA wire reference**](reference/vma-wire-reference.md) + [**Firmware updates**](guide/firmware-updates.md) + [`VmaClient`](../src/MonitorControlSDK/Clients/VmaClient.cs).
   - **MCU / physical UI:** [examples/arduino-knobs-brightness-contrast](../examples/arduino-knobs-brightness-contrast/) (HTTP to the web API).
   - **Mapping `references/` to the shipped SDK:** [**references parity**](reference/references-parity.md) + [**VMC literal appendix**](reference/appendices/vmc-stat-tokens-from-references.txt).

## Reference (exhaustive opcode data)

| Topic | Document |
|--------|-----------|
| **`references/` ↔ shipped SDK (full parity map)** | [reference/references-parity.md](reference/references-parity.md) |
| **PVM-740 programmer manual (ManualsLib excerpt) — synthesized in repo** | [reference/pvm-740-programmer-manual-synthesis.md](reference/pvm-740-programmer-manual-synthesis.md) + [appendices/pvm-740-vmc-catalog-from-manual.txt](reference/appendices/pvm-740-vmc-catalog-from-manual.txt) |
| SDCP negative acknowledgements (numeric codes) | [reference/sdcp-error-codes.md](reference/sdcp-error-codes.md) |
| SDAP advertisement field map | [spec/sdap-overview.md](spec/sdap-overview.md) + [`SdapAdvertisementPacket`](../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs) |
| VMS: constant tree (323 `CMD_*` lines) | [appendices/vms-opcode-constants.txt](reference/appendices/vms-opcode-constants.txt) (generated from [`LegacyVmsContainer`](../src/MonitorControlSDK/Internal/LegacyVmsContainer.cs)) |
| VMS: every `send*` entry point (90 methods) | [appendices/vms-engine-send-methods.txt](reference/appendices/vms-engine-send-methods.txt) (from [`VmsCommandEngine`](../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs)) |
| VMC: **all** `STATget` / `STATset` literals from `references/**/*.cs` | [appendices/vmc-stat-tokens-from-references.txt](reference/appendices/vmc-stat-tokens-from-references.txt) (`bash scripts/regen-appendices.sh`) |
| VMA: adjustment / service / direct-backlight layout | [reference/vma-wire-reference.md](reference/vma-wire-reference.md) |
| External manuals & adjacent protocols (PVM-740, projectors, community libs) | [reference/external-sources.md](reference/external-sources.md) |

## Guides

| Topic | Document |
|--------|-----------|
| **CI/CD (GitHub Actions + GitLab self-hosted)** | [ci-cd.md](ci-cd.md) |
| **HTTP API + browser UI + Python gateway** | [guide/web-api-and-python-gateway.md](guide/web-api-and-python-gateway.md) |
| **OpenAPI → C client (codegen)** | [guide/openapi-codegen.md](guide/openapi-codegen.md) |
| Firmware VMA sequence and SDK API | [guide/firmware-updates.md](guide/firmware-updates.md) |
| TCP/UDP surfaces, Telnet/SSH, SNMP wording | [guide/network-and-debug.md](guide/network-and-debug.md) (includes SDCP UDP **53484**) |
| Broadcast-style REPL sample | [spec/broadcast-realtime-control.md](spec/broadcast-realtime-control.md) |

## Repository map

| Path | Role |
|------|------|
| [`src/MonitorControlSDK/`](../src/MonitorControlSDK/) | Library: protocol, transport, clients |
| [`src/MonitorControl.Web/`](../src/MonitorControl.Web/) | **HTTP JSON API**, Swagger, browser UI |
| [`src/MonitorControl.Cli/`](../src/MonitorControl.Cli/) | `monitorctl` |
| [`examples/python-service/`](../examples/python-service/) | Optional Python `uvicorn` gateway to the HTTP API |
| [`examples/arduino-knobs-brightness-contrast/`](../examples/arduino-knobs-brightness-contrast/) | ESP32/ESP8266 sketch: ADC → `POST /api/vmc/set` |
| [`examples/esp32-sdcp-vmc/`](../examples/esp32-sdcp-vmc/) | ESP32: ADC → native TCP SDCP / VMC (no HTTP) |
| [`samples/`](../samples/) | Runnable examples (including [`Sample.UdpVmcBroadcast`](../samples/Sample.UdpVmcBroadcast/) for UDP SDCP VMC) |
| [`tests/`](../tests/) | Unit tests |
| [`docs/plan/00-inventory.md`](plan/00-inventory.md) | Which **source files** implement which concern |
| [`scripts/regen-appendices.sh`](../scripts/regen-appendices.sh) | Regenerate VMS + VMC appendix text files |

## Legal

Protocol documentation is for interoperability. Trademarks belong to their owners. Validate behavior on your hardware and firmware revision before production use.
