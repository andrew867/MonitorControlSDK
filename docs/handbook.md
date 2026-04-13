# MonitorControl engineering handbook

This document is the **narrative spine** for the repository: what the system is, how the wire protocols relate to the C# implementation, which runnable artifacts exist, and where to read deeper detail. **Operational protocol rules** needed to implement or audit a client are in [`docs/reference/`](reference/) and [`docs/spec/`](spec/); you do not need external PDFs for the SDAP/SDCP/VMC/VMS/VMA surfaces this project exercises.

## 1. Purpose and scope

**MonitorControl** is a **.NET 8** solution that speaks the same **LAN discovery and control** dialects as compatible **professional monitors**: **SDAP** (UDP advertisements) and **SDCP** (structured frames on TCP, with an optional **UDP** path for VMC broadcast). The shipped **NuGet** package is **`MonitorControl.Sdk`** (`MonitorControl.*` namespaces). Optional hosts wrap the library in **HTTP + OpenAPI** for integration with browsers, Python, or embedded HTTP stacks.

**Out of scope:** consumer television stacks (for example BRAVIA Simple IP Control on different ports and framing), projector-only transports (for example ADCP on 53595), and any vendor binary control daemons. This tree is **plain managed C#** over sockets.

## 2. Trust boundaries and safety

| Boundary | Risk | Mitigation in this repo |
|----------|------|-------------------------|
| LAN / SDCP | Any host that can reach the monitor can send control frames if the device accepts them. | Document firewall posture; never expose SDCP ports to the open Internet without a VPN. |
| UDP VMC **Group / All** | One datagram can affect **many** chassis. | CLI and HTTP document scope; operators choose subnet broadcast vs global. |
| **VMA firmware** | Bricking or inconsistent firmware if used incorrectly. | HTTP firmware routes require **config + header** gate ([`WireFormat.FirmwareGate`](../src/MonitorControl.Web/WireFormat.cs)); see [firmware-updates.md](guide/firmware-updates.md). |

## 3. Protocol stack (self-contained reading order)

| Layer | Topic | Authoritative doc | Primary types |
|-------|--------|-------------------|----------------|
| Discovery | SDAP UDP **53862**, field layout | [spec/sdap-overview.md](spec/sdap-overview.md), [reference/pvm-740-programmer-manual-synthesis.md](reference/pvm-740-programmer-manual-synthesis.md) | [`SdapAdvertisementPacket`](../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs), [`SdapDiscovery`](../src/MonitorControlSDK/Transport/SdapDiscovery.cs) |
| Framing | SDCP v3/v4, ports **53484**, item numbers | [reference/sdcp-framing-and-items.md](reference/sdcp-framing-and-items.md) | [`SdcpMessageBuffer`](../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs), [`SdcpConnection`](../src/MonitorControlSDK/Transport/SdcpConnection.cs) |
| Picture / ASCII | VMC `STATget` / `STATset` | [reference/vmc-command-surface.md](reference/vmc-command-surface.md) | [`VmcClient`](../src/MonitorControlSDK/Clients/VmcClient.cs), [`LegacyVmcContainer`](../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs) |
| Factory / structured | VMS | [reference/vms-overview.md](reference/vms-overview.md), [spec/vms-command-matrix.md](spec/vms-command-matrix.md) | [`VmsClient`](../src/MonitorControlSDK/Clients/VmsClient.cs), [`VmsCommandEngine`](../src/MonitorControlSDK/Protocol/VmsCommandEngine.cs) |
| Service / dangerous | VMA | [reference/vma-wire-reference.md](reference/vma-wire-reference.md) | [`VmaClient`](../src/MonitorControlSDK/Clients/VmaClient.cs), [`LegacyVmaContainer`](../src/MonitorControlSDK/Internal/LegacyVmaContainer.cs) |
| Errors | Negative acknowledgements | [reference/sdcp-error-codes.md](reference/sdcp-error-codes.md) | [`SdcpErrorCodes`](../src/MonitorControlSDK/Protocol/SdcpErrorCodes.cs) |
| UDP shading | VMC broadcast | Same as SDCP + PVM-740 synthesis | [`VmcUdpBroadcastClient`](../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs), [`SdcpUdpBroadcastTransport`](../src/MonitorControlSDK/Transport/SdcpUdpBroadcastTransport.cs) |

**Optional cross-checks** against third-party manuals and community implementations are listed in [reference/external-sources.md](reference/external-sources.md); they are **not** required to understand or extend this codebase.

## 4. Implementation map (code ↔ concern)

See [plan/00-inventory.md](plan/00-inventory.md) for a file-level index. At a glance:

1. **Framing** — `SdcpMessageBuffer` builds headers and payload regions; V3 vs V4 paths are explicit.
2. **Containers** — `Legacy*` types are **internal wire builders** (opcode layout preserved for interoperability); they are not a public DSL.
3. **Clients** — `VmcClient`, `VmsClient`, `VmaClient` send/receive over `ISdcpTransport`.
4. **Transports** — TCP (`SdcpConnection`), UDP broadcast (`SdcpUdpBroadcastTransport`), SDAP listen.

## 5. Operator surfaces

| Surface | Path | Audience |
|---------|------|----------|
| **Class library** | [`src/MonitorControlSDK/`](../src/MonitorControlSDK/) | Application developers (NuGet) |
| **CLI** `monitorctl` | [`src/MonitorControl.Cli/`](../src/MonitorControl.Cli/) | Operators, scripts |
| **HTTP + OpenAPI** | [`src/MonitorControl.Web/`](../src/MonitorControl.Web/) | Web stacks, gateways, codegen |
| **.NET samples** | [`samples/`](../samples/) | Copy-paste starting points |
| **MCU / Python examples** | [`examples/`](../examples/) | Non-.NET integration patterns |

### 5.1 CLI commands

Implemented in [`Program.cs`](../src/MonitorControl.Cli/Program.cs) (`System.CommandLine`).

| Command | Transport | Purpose |
|---------|-----------|---------|
| `discover` | SDAP listen | Print advertisements (optional `--bind`, `--filter`) |
| `vmc --host <ip> <field>` | TCP SDCP | `STATget` one field |
| `vmc-broadcast` | UDP SDCP | `STATset` / other VMC category to Group or All (`--scope`, `--group-id`, `--broadcast`, `--port`, `--local-bind`) |
| `vms-info --host <ip>` | TCP SDCP | VMS product info + common packaged status (hex preview) |
| `vma-version --host <ip>` | TCP SDCP | VMA control software version read |

### 5.2 HTTP API and push

Full route table, query parameters, JSON bodies, and firmware gate: [guide/web-api-and-python-gateway.md](guide/web-api-and-python-gateway.md). **SSE** and **WebSocket** endpoints **poll** `STATget` on the server; the monitor does not initiate HTTP.

### 5.3 OpenAPI

- **Canonical committed document:** [`openapi/monitorcontrol.openapi.json`](../openapi/monitorcontrol.openapi.json) (regenerate with `bash scripts/fetch-openapi.sh` before release when routes change).
- **Live document** while the web host runs: `GET /swagger/v1/swagger.json`.
- **Codegen:** [guide/openapi-codegen.md](guide/openapi-codegen.md).

## 6. Diagrams

Mermaid diagrams for stacks, discovery vs control, VMC sequences, HTTP bridging, SSE/WebSocket polling, and knob-style control: [diagrams/monitor-control-flows.md](diagrams/monitor-control-flows.md).

## 7. Quality assurance

- **Unit tests:** [`tests/MonitorControlSDK.Tests/`](../tests/MonitorControlSDK.Tests/).
- **Strategy:** [testing/strategy.md](testing/strategy.md), [testing/broadcast-realtime-control-tests.md](testing/broadcast-realtime-control-tests.md).
- **CI/CD:** [ci-cd.md](ci-cd.md) (GitHub Actions + GitLab).

## 8. Versioning and release

- **Package / assembly version:** `<Version>` in [`MonitorControlSDK.csproj`](../src/MonitorControlSDK/MonitorControlSDK.csproj).
- **Git tags:** `vMAJOR.MINOR.PATCH` aligned with that version.
- **Checklist:** [shipping/release-checklist.md](shipping/release-checklist.md).

## 9. Legal

Protocol documentation supports interoperability. Trademarks belong to their owners. Validate on your hardware and firmware revision before production deployment.
