# SDCP framing and item numbers

All behavior described here is implemented in [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs) and sent over TCP by [`SdcpConnection`](../../src/MonitorControlSDK/Transport/SdcpConnection.cs) (default port **53484**).

**Public programmer manual excerpt (consolidated):** [pvm-740-programmer-manual-synthesis.md](pvm-740-programmer-manual-synthesis.md) — ports, SDAP/SDCP rules, pacing, VMC categories, and PVM-740 command spellings ([appendices/pvm-740-vmc-catalog-from-manual.txt](appendices/pvm-740-vmc-catalog-from-manual.txt)).

## Transport

| Service | Protocol | Port | Type in code |
|---------|-----------|------|----------------|
| SDCP control | TCP | 53484 | `SdcpConnection.DefaultPort` |
| SDAP discovery | UDP | 53862 | `SdapDiscovery.DefaultPort` |

## V3 frame (VMC, VMA)

### Thirteen-byte prefix (before `Data`)

| Offset | Size | Field | Typical values (PVM-740 excerpt + `SdcpMessageBuffer`) |
|--------|------|-------|----------------------------------------------------------|
| 0 | 1 | Version | **`03h`** (SDCP v3) |
| 1 | 1 | Category | **`0Bh`** monitor category |
| 2–5 | 4 | Community | ASCII **`SONY`** (`53h 4Fh 4Eh 59h`) — case-sensitive |
| 6 | 1 | Group ID | **Single / P2P:** `00h`. **Group:** `01h`–`63h` (1–99 decimal per PVM-740 excerpt). **All:** `FFh`. |
| 7 | 1 | Unit ID | **Single:** target unit id (`setSingleConnection`). **Group / P2P:** `00h`. **All:** `FFh`. |
| 8 | 1 | Request / response | Request **`00h`**; response **OK `01h`** / **NG `00h`** (`SDCP_COMMAND_*`) |
| 9–10 | 2 | Item number (big-endian) | **`B000h`** VMC ASCII (`SdcpV3ItemVideoMonitorControl`); **`B001h`** built-in-controller VMC (`SdcpV3ItemVideoMonitorControlBuiltIn`) |
| 11–12 | 2 | Data length (big-endian) | Byte count **n** of following `Data`; PVM-740 excerpt max **499** (`01F3h`); buffer allocation may be larger for legacy interop |

Then **`Data[n]`** — for VMC/VMA, the payload builders write ASCII or binary into `packetData`.

- **Payload allocation:** up to **960** bytes (`packetData`); total wire read buffer **`maxSize` = 973** bytes (13 + 960).
- **VMC:** ASCII `STATget` / `STATset` (and other categories if you build them) via [`LegacyVmcContainer`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs) and [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs). Default item **`0xB000`** only; use constants for **`0xB001`** if you extend the client.
- **VMA:** **0xF000** — binary jig/service/direct payloads via [`LegacyVmaContainer`](../../src/MonitorControlSDK/Internal/LegacyVmaContainer.cs) and [`VmaClient`](../../src/MonitorControlSDK/Clients/VmaClient.cs).

**Request/response byte** (offset 8): see `SDCP_COMMAND_REQUEST`, `SDCP_COMMAND_RESPONSE_OK`, `SDCP_COMMAND_RESPONSE_NG` in `SdcpMessageBuffer`.

## V4 frame (VMS)

- **Header:** 37 bytes — `setSdcpV4PacketHeader`, `setupVmsPacketHeader`.
- **Item number** for VMS: **0xB900** (bytes 33–34 in V4 layout).
- Helpers on [`LegacyVmsContainer`](../../src/MonitorControlSDK/Internal/LegacyVmsContainer.cs) build sub-opcodes; [`VmsCommandEngine`](../../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs) sequences send/receive.

## Targeting (group / unit)

Bytes **6–7** (V3) encode single / group / all / P2P targeting. Writers: `setSingleConnection`, `setGroupConnection`, `setAllConnection`, `setP2pConnection` on `SdcpMessageBuffer`. Values match the tables described in public **PVM-740 Interface Manual for Programmers** (SDCP section) — see [external-sources.md](external-sources.md).

## Other item constants (internal / future)

`SdcpMessageBuffer` also defines private `SDCP_COMMAND_ITEM_*` values (e.g. monitor, system setting, service adjustment, 4K PC application). These exist for parity with the original wire catalog; VMC/VMS/VMA paths above are what this SDK exercises today.

## Public documentation (similar protocol)

Sony’s **PVM-740** programmer manual excerpt (ManualsLib) and community SDCP libraries match the header above. The repository now **hosts a full synthesis** so you do not need the live ManualsLib page for core rules: [pvm-740-programmer-manual-synthesis.md](pvm-740-programmer-manual-synthesis.md). Additional URLs: [external-sources.md](external-sources.md). **Always validate** on your chassis.
