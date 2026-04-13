# VMS command matrix

All structured VMS operations from legacy `VmsCommand` are implemented on **`VmsCommandEngine`** ([VmsCommandEngine.cs](../../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs)).

## Usage pattern

1. `SdcpMessageBuffer` — call `setSdcpV4PacketHeader`, `setupVmsPacketHeader`, `clearContainer`, then payload builders on `LegacyVmsContainer` from `createVmsContainer()`.
2. `ISdcpTransport.sendPacketV4` / `receivePacketV4` — typically via `SdcpConnection`.

Or use **`VmsClient`** for a small curated subset, and **`VmsClient.Engine`** for the full `VmsCommandEngine` surface.

## Method catalog (auto-aligned with source)

Every `public int send…` / `recvVmsPacket` / `checkVmsRecvPacketError` on `VmsCommandEngine` maps 1:1 to the legacy tool chain. Product-specific variants (BVM-X, PVM-X, LMD-A, etc.) are separate methods preserving the legacy sub-opcode bytes in `LegacyVmsContainer`.

Regenerate a flat list locally:

```bash
rg "^\tpublic int " src/MonitorControlSDK/Protocol/VmsCommandEngine.cs
```

## Payload encoding

Floating-point triplets for panel correction use `convVmsFloatValue` / `convVmsRgbFloatStructure` (big-endian component encoding). Unit tests cover round-trip in `VmsFloatCodecTests`.
