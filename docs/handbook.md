# MonitorControl engineering handbook

This document is the **narrative spine** for the repository: what the system is, how the wire protocols relate to the C# implementation, which runnable artifacts exist, and where to read deeper detail. **Operational protocol rules** needed to implement or audit a client are in [`docs/reference/`](reference/) and [`docs/spec/`](spec/); you do not need external PDFs for the SDAP/SDCP/VMC/VMS/VMA surfaces this project exercises.

## 1. Purpose and scope

**MonitorControl** is a **.NET 8** solution that speaks the same **LAN discovery and control** dialects as compatible **professional monitors**: **SDAP** (UDP advertisements) and **SDCP** (structured frames on TCP, with an optional **UDP** path for VMC broadcast). The shipped **NuGet** package is **`MonitorControl.Sdk`** (`MonitorControl.*` namespaces). Optional hosts wrap the library in **HTTP + OpenAPI** for integration with browsers, Python, or embedded HTTP stacks.

**In-tree MCU examples:** (1) **ESP32 / ESP8266** over **HTTP** to `MonitorControl.Web` — minimal SDCP knowledge on the MCU. (2) **ESP32** over **native TCP SDCP** — full **V3** framing with item **`0xB000`** (default) implemented in firmware to match the SDK; use **`0xB001`** on hardware that requires the built-in-controller item (see [`examples/esp32-sdcp-vmc/monitor_knobs_sdcp.ino`](../examples/esp32-sdcp-vmc/monitor_knobs_sdcp.ino)).

**Out of scope:** consumer television stacks (for example BRAVIA Simple IP Control on different ports and framing), projector-only transports (for example ADCP on 53595), and any vendor binary control daemons. This tree is **plain managed C#** over sockets (plus optional Arduino sketches).

## 2. Trust boundaries and safety

| Boundary | Risk | Mitigation in this repo |
|----------|------|-------------------------|
| LAN / SDCP | Any host that can reach the monitor can send control frames if the device accepts them. | Document firewall posture; never expose SDCP ports to the open Internet without a VPN. |
| UDP VMC **Group / All** | One datagram can affect **many** chassis. | CLI and HTTP document scope; operators choose subnet broadcast vs global. |
| **VMA firmware** | Bricking or inconsistent firmware if used incorrectly. | HTTP firmware routes require **config + header** gate ([`WireFormat.FirmwareGate`](../src/MonitorControl.Web/WireFormat.cs)); see [firmware-updates.md](guide/firmware-updates.md). |
| **On-air shading** | Wrong `STATset` during live production. | Samples and REPL document risk; ESP32 README describes model-specific tokens. |

## 3. Protocol stack (self-contained reading order)

| Layer | Topic | Authoritative doc | Primary types |
|-------|--------|-------------------|----------------|
| Discovery | SDAP UDP **53862**, field layout | [spec/sdap-overview.md](spec/sdap-overview.md), [reference/pvm-740-programmer-manual-synthesis.md](reference/pvm-740-programmer-manual-synthesis.md) | [`SdapAdvertisementPacket`](../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs), [`SdapDiscovery`](../src/MonitorControlSDK/Transport/SdapDiscovery.cs) |
| Framing | SDCP v3/v4, ports **53484**, item numbers | [reference/sdcp-framing-and-items.md](reference/sdcp-framing-and-items.md) | [`SdcpMessageBuffer`](../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs), [`SdcpConnection`](../src/MonitorControlSDK/Transport/SdcpConnection.cs) |
| Picture / ASCII | VMC `STATget` / `STATset` | [reference/vmc-command-surface.md](reference/vmc-command-surface.md), [spec/vmc-string-catalog.md](spec/vmc-string-catalog.md) | [`VmcClient`](../src/MonitorControlSDK/Clients/VmcClient.cs), [`LegacyVmcContainer`](../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs) |
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
| **.NET samples** | [`samples/`](../samples/) | Copy-paste starting points — [`samples/README.md`](../samples/README.md) |
| **MCU / Python examples** | [`examples/`](../examples/) | Non-.NET integration — [`examples/README.md`](../examples/README.md) |

### 5.1 CLI commands

Implemented in [`Program.cs`](../src/MonitorControl.Cli/Program.cs) (`System.CommandLine`).

| Command | Transport | Purpose |
|---------|-----------|---------|
| `discover` | SDAP listen | Print advertisements (optional `--bind`, `--filter`) |
| `vmc --host <ip> <field>` | TCP SDCP | `STATget` one field; optional `--sdcp-unit`, `--vmc-item` (B000/monitor vs B001/builtIn) |
| `vmc-broadcast` | UDP SDCP | `STATset` / other VMC category to Group or All (`--scope`, `--group-id`, `--broadcast`, `--port`, `--local-bind`, optional `--vmc-item`) |
| `vms-info --host <ip>` | TCP SDCP | VMS product info + common packaged status (hex preview) |
| `vma-version --host <ip>` | TCP SDCP | VMA control software version read |

### 5.2 HTTP API and push

Full route table, query parameters, JSON bodies, and firmware gate: [guide/web-api-and-python-gateway.md](guide/web-api-and-python-gateway.md). **SSE** and **WebSocket** endpoints **poll** `STATget` on the server; the monitor does not initiate HTTP.

### 5.3 OpenAPI

- **Canonical committed document:** [`openapi/monitorcontrol.openapi.json`](../openapi/monitorcontrol.openapi.json) (regenerate with `bash scripts/fetch-openapi.sh` before release when routes change).
- **Live document** while the web host runs: `GET /swagger/v1/swagger.json`.
- **Codegen:** [guide/openapi-codegen.md](guide/openapi-codegen.md).

### 5.4 Embedded paths: HTTP vs native SDCP on ESP32

| Example | File(s) | Last hop to monitor | When to use |
|---------|---------|---------------------|-------------|
| **HTTP knobs** | [`examples/arduino-knobs-brightness-contrast/monitor_knobs_http.ino`](../examples/arduino-knobs-brightness-contrast/) | WiFi → JSON → **MonitorControl.Web** → TCP SDCP | ESP8266, TLS/auth at gateway, smallest firmware risk on wire format |
| **Native SDCP** | [`examples/esp32-sdcp-vmc/monitor_knobs_sdcp.ino`](../examples/esp32-sdcp-vmc/) | WiFi → **WiFiClient** → **TCP :53484** SDCP v3 | No PC in path; you maintain byte-for-byte header compatibility with the SDK |

**Wire parity (native sketch vs C#):** The sketch’s `buildVmcPacket` / `buildVmcStatSetTail` writes: `wire[0]=3` (SDCP v3), `wire[1]=11` (monitor category), `wire[2..5]="SONY"`, `wire[6]=wire[7]=0` (single connection), `wire[8]=0` (request), `wire[9..10]=0xB0,0x00` (item VMC), `wire[11..12]` big-endian data length, then ASCII payload — matching [sdcp-framing-and-items.md](reference/sdcp-framing-and-items.md) and [`SdcpMessageBuffer`](../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs) layout for VMC. Response handling checks **`rx[8] == 1`** for OK before treating the exchange as success (same semantics as `SDCP_COMMAND_RESPONSE_OK` in C#). Buffer cap **973** bytes matches SDK read sizing (`kSdcpMax` in sketch).

**Modes on native ESP32:** **PICTURE** (brightness/contrast), **RGB_GAIN** (`RGAIN`/`GGAIN`/`BGAIN`), **GRADE** (aperture/chroma/phase ranges per README). **MODE** / **CAL** / **POWER** buttons and serial `cap` / `flat` commands are documented in the example README.

**Provisioning:** The same example now ships a **captive WiFi AP** (`MonitorCtrl-*` / default password in sketch) plus an **HTML5** page (`config_portal.h`) for STA credentials, **SDAP discovery** (UDP **53862**) to pick **Connection IP**, and a **STA-side** config server on **port 8080** — see [`examples/esp32-sdcp-vmc/README.md`](../examples/esp32-sdcp-vmc/README.md).

### 5.5 Python gateway (`examples/python-service`)

Proxies **`/api/*`** to the .NET base URL and streams **`GET /api/events/...`** (SSE) through to the client. **`/ws/monitor-watch` is not proxied** — browsers or tools must open WebSocket connections to the **MonitorControl.Web** port. Details: [examples/python-service/README.md](../examples/python-service/README.md), [diagrams/monitor-control-flows.md](diagrams/monitor-control-flows.md) (Python gateway figure).

## 6. Diagrams

Mermaid diagrams (including **ESP32 native sequence**, **Python proxy vs WebSocket**, **dual physical UI paths**, and **BroadcastControl REPL**): [diagrams/monitor-control-flows.md](diagrams/monitor-control-flows.md).

## 7. Quality assurance

- **Unit tests:** [`tests/MonitorControlSDK.Tests/`](../tests/MonitorControlSDK.Tests/).
- **Strategy:** [testing/strategy.md](testing/strategy.md), [testing/broadcast-realtime-control-tests.md](testing/broadcast-realtime-control-tests.md).
- **CI/CD:** [ci-cd.md](ci-cd.md) (GitHub Actions + GitLab). `dotnet build MonitorControl.sln` builds all solution projects including **samples** (see `MonitorControl.sln`).

## 8. Versioning and release

- **Package / assembly version:** `<Version>` in [`MonitorControlSDK.csproj`](../src/MonitorControlSDK/MonitorControlSDK.csproj).
- **Git tags:** `vMAJOR.MINOR.PATCH` aligned with that version.
- **Checklist:** [shipping/release-checklist.md](shipping/release-checklist.md).

## 9. Legal

Protocol documentation supports interoperability. Trademarks belong to their owners. Validate on your hardware and firmware revision before production deployment.
