# VMS (SDCP v4 item 0xB900) overview

Structured **factory / calibration / network / packaged status** commands use the V4 header and VMS item **0xB900**. Implementation:

- Payload layout: [`LegacyVmsContainer`](../../src/MonitorControlSDK/Internal/LegacyVmsContainer.cs) — **323** `CMD_*` opcode constants defining the full tree.
- Send/receive orchestration: [`VmsCommandEngine`](../../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs) — **90** `public int send…` methods (plus `recvVmsPacket`, error checks).
- Thin curated API: [`VmsClient`](../../src/MonitorControlSDK/Clients/VmsClient.cs) (`Engine` exposes the full engine).

## Top-level command classes (`LegacyVmsContainer`)

These are the **first byte** of the VMS sub-protocol (see source for the full nested constants):

| Byte value | Name | Domain |
|------------|------|--------|
| 0 | `CMD_COMMON` | Product info, control start, software version, system condition |
| 4 | `CMD_HOST_CONTROL` | Host permission / set rights |
| 8 | `CMD_SYSTEM_CONFIGURATION` | Network, power, on-screen, datetime, password, factory restore |
| 16 | `CMD_ADJUSTMENT` | Auto chroma, picture manual, presets, auto/manual color temp, luminance probe, … |
| 24 | `CMD_INFORMATION` | Information queries |
| 32–39 | `CMD_UNIVERSAL_FUNCTION_*` | UF common / display / signal / maker / LCD / scaling / PIP / analog |
| 48, 50, 52 | Display configuration branches | Input, maker, color customizing |
| 64–69 | Display function branches | PIP, gamut error, internal signal, parallel remote, various |
| 80 | `CMD_COPY_SYSTEM` | Copy system |
| 84 | `CMD_AREA_SETTING` | Area setting |
| 96 | `CMD_PACKEGED_STATUS` | Packaged status reads |

## Exhaustive listings

Machine-generated from this repo (regenerate after editing `LegacyVmsContainer` / `VmsCommandEngine`):

- [appendices/vms-opcode-constants.txt](appendices/vms-opcode-constants.txt)
- [appendices/vms-engine-send-methods.txt](appendices/vms-engine-send-methods.txt)

## Float encoding

RGB / panel correction floats use big-endian helpers — see `convVmsFloatValue` in `VmsCommandEngine` and tests in `VmsFloatCodecTests`.
