# Network surfaces, debug, Telnet, SSH

## What this SDK implements

| Surface | Protocol | Port | Code |
|---------|-----------|------|------|
| SDCP control | TCP | 53484 | [`SdcpConnection`](../../src/MonitorControlSDK/Transport/SdcpConnection.cs) |
| SDCP VMC (UDP Group / All) | UDP | 53484 | [`SdcpUdpBroadcastTransport`](../../src/MonitorControlSDK/Transport/SdcpUdpBroadcastTransport.cs), [`VmcUdpBroadcastClient`](../../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs) |
| SDAP advertisement | UDP | 53862 | [`SdapDiscovery`](../../src/MonitorControlSDK/Transport/SdapDiscovery.cs) |

The **PVM-740 programmer manual** excerpt also lists **FTP TCP 21** on the monitor; this SDK does **not** implement FTP — see [reference/pvm-740-programmer-manual-synthesis.md](../reference/pvm-740-programmer-manual-synthesis.md).

There is **no** Telnet client, **no** SSH client, and **no** serial port layer in this repository.

## Telnet / SSH

Across the **C# sources in this repo**, there are **no** Telnet or SSH client implementations for the monitor. Public **BRAVIA** “Simple IP Control” documentation describes **TCP 20060** with a **24-byte** fixed message format — that is a **different** product line and protocol from SDCP on 53484 ([BRAVIA Professional Displays — Simple IP control](https://pro-bravia.sony.net/remote-display-control/simple-ip-control/)).

**Conclusion for integrators:** assume remote access is **SDCP + SDAP** unless your specific model’s **hardware manual** documents another maintenance channel.

## SNMP

The VMS **system configuration** branch includes opcodes with **SNMP** in the constant *names* (e.g. contact, name, location, trap mode) in [`LegacyVmsContainer`](../../src/MonitorControlSDK/Internal/LegacyVmsContainer.cs). Those are **parameters carried inside SDCP VMS payloads**, not evidence that the monitor exposes a standalone SNMP agent on UDP 161 in all configurations. Treat per-model documentation as authoritative.

## Practical debugging

1. **Packet capture** on a mirror port (Wireshark) filtering `tcp.port == 53484`, `udp.port == 53484` (SDCP datagrams), or `udp.port == 53862` (SDAP).
2. **Hex dump** helpers in [`monitorctl`](../../src/MonitorControl.Cli/Program.cs) (`vms-info` subcommand dumps leading payload bytes).
3. **Unit tests** for framing and float codecs under [`tests/MonitorControlSDK.Tests/`](../tests/MonitorControlSDK.Tests/).

## External manuals

See [reference/external-sources.md](../reference/external-sources.md) for programmer manuals and community SDCP implementations useful for cross-checking headers.
