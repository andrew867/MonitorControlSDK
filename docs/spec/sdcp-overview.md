# SDCP overview

**SDCP** (Sony Display Control Protocol in this codebase) uses **TCP** to port **53484** by default (`SdcpConnection.DefaultPort`).

## Versions

The legacy stack supports two on-wire layouts from a single logical session:

- **V3 header** (13 bytes) + payload up to **960** bytes (`SdcpMessageBuffer.maxSize` total 973 bytes including header). Used for **VMC** (ASCII commands) and **VMA** (binary service/adjustment).
- **V4 header** (37 bytes) + payload; used for **VMS** structured commands. Item number in header is **0xB900** (`setupVmsPacketHeader`).

## Header layout (V3, indices)

| Offset | Size | Meaning |
|--------|------|---------|
| 0 | 1 | Version (3 for VMC/VMA) |
| 1 | 1 | Category (11) |
| 2–5 | 4 | ASCII `SONY` community marker |
| 6 | 1 | Group ID (targeting mode) |
| 7 | 1 | Unit ID |
| 8 | 1 | Request/response flag |
| 9–10 | 2 | Item number (big-endian); VMC uses **0xB000** (176, 0); VMA uses **0xF000** (240, 0) |
| 11–12 | 2 | Payload length (big-endian) |

Targeting modes (`setSingleConnection`, `setGroupConnection`, `setAllConnection`, `setP2pConnection`) write bytes 6–7 exactly as the legacy `SdcpPacket.setIDsByConnectType` implementation.

## Header layout (V4, indices)

V4 extends the header to **37** bytes. Group/unit move to offsets **30–31**; request/response at **32**; item at **33–34** (VMS uses **0xB900**); payload length at **35–36**; payload starts at **37**.

## Error codes

Numeric **ushort** values are defined on the legacy `SdcpPacket` class and mirrored in `SdcpErrorCodes` (subset). Devices may return these in negative acknowledgements depending on firmware.

## Reference implementation

Authoritative C#: [MonitorNetwork/MonitorNetwork/SdcpPacket.cs](../../MonitorNetwork/MonitorNetwork/SdcpPacket.cs) — ported to [SdcpMessageBuffer.cs](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs).
