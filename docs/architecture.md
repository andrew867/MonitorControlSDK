# Architecture

## What ships

The **Sony.MonitorControl** assembly is plain **.NET 8** C#: SDAP/SDCP framing, TCP/UDP transports, VMS/VMC/VMA payload builders, and typed clients. There is **no** P/Invoke into external control binaries.

## Naming: `Legacy*` types

Types such as `LegacyVmcContainer`, `LegacyVmsContainer`, `LegacyVmaContainer`, and `VmsCommandEngine` are **internal wire builders** whose field order and opcodes match the historical interoperating tools this stack was verified against. They live **only** in this repository under `src/MonitorControlSDK/`.

## Layering

1. **Buffers** — `SdcpMessageBuffer` (V3/V4 layouts, lengths, community bytes).
2. **Containers** — `Legacy*` types write opcode bytes into the buffer data region.
3. **Engines / clients** — `VmsCommandEngine`, `VmcClient`, `VmaClient` orchestrate send/receive over `ISdcpTransport`.
4. **Transports** — `SdcpConnection` (TCP), `StreamSdcpTransport` (`Stream` for tests or proxies).

## Documentation

Human-facing documentation is entirely under [`docs/`](index.md); generated opcode appendices live under [`docs/reference/appendices/`](reference/appendices/README.md).
