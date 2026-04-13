# MonitorControlSDK documentation

Single source of truth for **Sony.MonitorControl** — SDAP discovery and SDCP control for compatible Sony professional monitors. Everything below is grounded in this repository’s C# sources or cited public materials.

## New here? (~10 minutes)

1. Read [**Quick start**](quickstart.md) — install .NET 8, build, run discovery, send your first VMC query.
2. Skim [**SDCP framing and item numbers**](reference/sdcp-framing-and-items.md) — ports, headers, V3 vs V4.
3. Pick your path:
   - **ASCII shading / menus:** [**VMC command surface**](reference/vmc-command-surface.md) + [`VmcClient`](../src/MonitorControlSDK/Clients/VmcClient.cs).
   - **Structured factory / setup:** [**VMS overview**](reference/vms-overview.md) + [`VmsCommandEngine`](../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs).
   - **Service / firmware (dangerous):** [**VMA wire reference**](reference/vma-wire-reference.md) + [**Firmware updates**](guide/firmware-updates.md) + [`VmaClient`](../src/MonitorControlSDK/Clients/VmaClient.cs).

## Reference (exhaustive opcode data)

| Topic | Document |
|--------|-----------|
| SDCP negative acknowledgements (numeric codes) | [reference/sdcp-error-codes.md](reference/sdcp-error-codes.md) |
| SDAP advertisement field map | [spec/sdap-overview.md](spec/sdap-overview.md) + [`SdapAdvertisementPacket`](../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs) |
| VMS: constant tree (323 `CMD_*` lines) | [appendices/vms-opcode-constants.txt](reference/appendices/vms-opcode-constants.txt) (generated from [`LegacyVmsContainer`](../src/MonitorControlSDK/Internal/LegacyVmsContainer.cs)) |
| VMS: every `send*` entry point (90 methods) | [appendices/vms-engine-send-methods.txt](reference/appendices/vms-engine-send-methods.txt) (from [`VmsCommandEngine`](../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs)) |
| VMA: adjustment / service / direct-backlight layout | [reference/vma-wire-reference.md](reference/vma-wire-reference.md) |
| External manuals & adjacent protocols (PVM-740, projectors, community libs) | [reference/external-sources.md](reference/external-sources.md) |

## Guides

| Topic | Document |
|--------|-----------|
| Firmware VMA sequence and SDK API | [guide/firmware-updates.md](guide/firmware-updates.md) |
| TCP/UDP surfaces, Telnet/SSH, SNMP wording | [guide/network-and-debug.md](guide/network-and-debug.md) |
| Broadcast-style REPL sample | [spec/broadcast-realtime-control.md](spec/broadcast-realtime-control.md) |

## Repository map

| Path | Role |
|------|------|
| [`src/MonitorControlSDK/`](../src/MonitorControlSDK/) | Library: protocol, transport, clients |
| [`src/MonitorControl.Cli/`](../src/MonitorControl.Cli/) | `monitorctl` |
| [`samples/`](../samples/) | Runnable examples |
| [`tests/`](../tests/) | Unit tests |
| [`docs/plan/00-inventory.md`](plan/00-inventory.md) | Which **source files** implement which concern |

## Legal

Protocol documentation is for interoperability. Trademarks belong to their owners. Validate behavior on your hardware and firmware revision before production use.
