# Implementation map (this repository)

This file maps **concerns** to **source files** in **MonitorControlSDK** only. There is no out-of-tree dependency for building, testing, or documenting the shipped SDK.

## Protocol and buffers

| Concern | Primary types |
|---------|----------------|
| SDCP v3/v4 framing, errors | [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs), [`SdcpErrorCodes`](../../src/MonitorControlSDK/Protocol/SdcpErrorCodes.cs) |
| SDAP advertisement parse | [`SdapAdvertisementPacket`](../../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs) |
| VMS payload tree | [`LegacyVmsContainer`](../../src/MonitorControlSDK/Internal/LegacyVmsContainer.cs), [`VmsCommandEngine`](../../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs) |
| VMC ASCII payloads | [`LegacyVmcContainer`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs) |
| VMA binary payloads | [`LegacyVmaContainer`](../../src/MonitorControlSDK/Internal/LegacyVmaContainer.cs) |

## Transport and clients

| Concern | Primary types |
|---------|----------------|
| TCP SDCP | [`SdcpConnection`](../../src/MonitorControlSDK/Transport/SdcpConnection.cs), [`StreamSdcpTransport`](../../src/MonitorControlSDK/Transport/StreamSdcpTransport.cs), [`ISdcpTransport`](../../src/MonitorControlSDK/Transport/ISdcpTransport.cs) |
| UDP SDCP VMC broadcast | [`SdcpUdpBroadcastTransport`](../../src/MonitorControlSDK/Transport/SdcpUdpBroadcastTransport.cs), [`VmcUdpBroadcastClient`](../../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs) |
| UDP SDAP listen | [`SdapDiscovery`](../../src/MonitorControlSDK/Transport/SdapDiscovery.cs) |
| Operator clients | [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs), [`VmsClient`](../../src/MonitorControlSDK/Clients/VmsClient.cs), [`VmaClient`](../../src/MonitorControlSDK/Clients/VmaClient.cs) |

## Operator surfaces

| Surface | Path |
|---------|------|
| HTTP / JSON / Swagger / browser UI / SSE / WebSocket | [`src/MonitorControl.Web/`](../../src/MonitorControl.Web/) (`MonitorApiExtensions`, `MonitorPushEndpoints`) |
| CLI | [`src/MonitorControl.Cli/`](../../src/MonitorControl.Cli/) |
| Samples | [`samples/`](../../samples/) |
| Python gateway (optional) | [`examples/python-service/`](../../examples/python-service/) |
| MCU → HTTP bridge (optional) | [`examples/arduino-knobs-brightness-contrast/`](../../examples/arduino-knobs-brightness-contrast/) |
| Control-flow diagrams | [`docs/diagrams/monitor-control-flows.md`](../diagrams/monitor-control-flows.md) |
| References ↔ shipped SDK (parity) | [`docs/reference/references-parity.md`](../reference/references-parity.md) |
| Appendix regeneration | [`scripts/regen-appendices.sh`](../../scripts/regen-appendices.sh) |

## Documentation entry

Start at [**docs/index.md**](../index.md).
