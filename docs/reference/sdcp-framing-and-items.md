# SDCP framing and item numbers

All behavior described here is implemented in [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs) and sent over TCP by [`SdcpConnection`](../../src/MonitorControlSDK/Transport/SdcpConnection.cs) (default port **53484**).

## Transport

| Service | Protocol | Port | Type in code |
|---------|-----------|------|----------------|
| SDCP control | TCP | 53484 | `SdcpConnection.DefaultPort` |
| SDAP discovery | UDP | 53862 | `SdapDiscovery.DefaultPort` |

## V3 frame (VMC, VMA)

- **Header:** 13 bytes — see `setupVmcPacketHeader` / `setupVma` in `SdcpMessageBuffer`.
- **Payload:** up to **960** bytes (`packetData` allocation); total buffer `maxSize` = **973** bytes.
- **Item number** (bytes 9–10, big-endian):
  - **VMC:** **0xB000** — ASCII `STATget` / `STATset` payloads via [`LegacyVmcContainer`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs) and [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs).
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

Sony’s **PVM-740** programmer manual excerpt on ManualsLib and community SDCP libraries describe the same header shape (version `03h`, category `0Bh`, `SONY` community). See [external-sources.md](external-sources.md) for URLs. **Always validate** against your device; firmware differs by chassis.
