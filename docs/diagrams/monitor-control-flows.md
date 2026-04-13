# Monitor control flows (Mermaid)

High-level views of **where data moves** and **how physical controls** reach the monitor. Ports and item numbers match [SDCP framing and item numbers](../reference/sdcp-framing-and-items.md). **HTTP routes** match [`MonitorApiExtensions`](../../src/MonitorControl.Web/MonitorApiExtensions.cs) + [`MonitorPushEndpoints`](../../src/MonitorControl.Web/MonitorPushEndpoints.cs) and the committed [OpenAPI snapshot](../../openapi/monitorcontrol.openapi.json).

**ESP32 native on-wire** reference implementation: [`monitor_knobs_sdcp.ino`](../../examples/esp32-sdcp-vmc/monitor_knobs_sdcp.ino) — full README: [`examples/esp32-sdcp-vmc/README.md`](../../examples/esp32-sdcp-vmc/README.md).

## Integration matrix (choose one column per deployment)

| Client | Discovery | Control transport | VMC path | Notes |
|--------|-------------|-------------------|----------|--------|
| **C# SDK** / **monitorctl** | `SdapDiscovery` UDP **53862** | TCP **53484**; optional UDP **53484** broadcast | `VmcClient`, `VmcUdpBroadcastClient` | Authoritative framing in `SdcpMessageBuffer` |
| **MonitorControl.Web** | `GET /api/sdap/discover` | Opens TCP **53484** per request (or UDP for `/api/vmc/broadcast`) | Same as SDK under the host | OpenAPI + static UI |
| **Python gateway** (`examples/python-service`) | Proxied `GET /api/sdap/discover` | Proxied `POST /api/*` | Same as .NET upstream | **SSE** proxied for `GET /api/events/*`; **WebSocket** is **not** proxied — use .NET port (see below) |
| **ESP32 HTTP** (`monitor_knobs_http.ino`) | Manual / SDAP elsewhere | WiFi → HTTP → gateway → TCP **53484** | JSON `POST /api/vmc/set` | No SDCP on MCU |
| **ESP32 native** (`monitor_knobs_sdcp.ino`) | **Not used in sketch** (set `MONITOR_HOST`) | WiFi → **TCP 53484** | Hand-built **SDCP v3** + item **0xB000** + ASCII | Parity with SDK header layout |
| **Sample.BroadcastControl** | Manual host arg | **Long-lived** TCP **53484** | REPL `get` / `set` | One session, many commands |

## End-to-end stacks (LAN)

Three different **MCU** shapes are supported in-tree: **HTTP to the gateway** (ESP32/ESP8266), **native TCP SDCP** (ESP32 only), or no MCU (PC / SBC). The CLI uses the **SDK** only (no HTTP).

```mermaid
flowchart LR
  subgraph Clients
    Web[Browser / SPA]
    Py[Python scripts]
    DotNet[.NET app / samples]
    Cli[monitorctl]
    McuHttp["ESP32 / ESP8266<br/>HTTP sketch"]
    EspSdcp["ESP32<br/>native SDCP sketch"]
  end

  subgraph Gateway_optional["Gateway optional"]
    WebApi["MonitorControl.Web"]
    PyGw["python-service :8000"]
  end

  subgraph Monitor_LAN["Monitor LAN"]
    SDAP["UDP SDAP :53862"]
    SDCP["TCP SDCP :53484"]
    SDCPu["UDP SDCP :53484<br/>VMC Group/All"]
    Mon[(Professional monitor)]
  end

  Web --> WebApi
  Py --> PyGw
  PyGw --> WebApi
  McuHttp --> WebApi

  WebApi --> SDCP
  WebApi --> SDCPu
  DotNet --> SDCP
  DotNet --> SDCPu
  Cli --> SDCP
  Cli --> SDCPu
  Cli --> SDAP

  EspSdcp --> SDCP

  SDAP -.->|listen| Web
  SDAP -.->|listen| Py
  SDAP -.->|listen| DotNet
  SDAP -.->|listen| Cli
  Mon --> SDAP
  Mon <--> SDCP
  SDCPu --> Mon
```

## Python gateway: what is proxied

[`examples/python-service/main.py`](../../examples/python-service/main.py) registers **`/api/{full_path:path}`** only. Static UI is served from Python; API calls go to `MONITOR_CONTROL_API_URL` (default `http://127.0.0.1:5080`).

```mermaid
flowchart TB
  Browser[Browser on :8000]

  subgraph Python[uvicorn :8000]
    Proxy["/api/* → httpx to .NET"]
    SSE["GET api/events/*<br/>StreamingResponse"]
    Static[StaticFiles /]
  end

  subgraph DotNet[MonitorControl.Web :5080]
    Api["/api/*"]
    Ws["/ws/monitor-watch<br/>WebSocket only here"]
  end

  Browser --> Static
  Browser --> Proxy
  Browser --> SSE
  Proxy --> Api
  SSE --> Api
  Browser -.->|use ws://…:5080/ws/…| Ws
```

**WebSocket clients** must use the **same host and port as the ASP.NET process** (for example `ws://127.0.0.1:5080/ws/monitor-watch?host=…`), not the Python port **8000**.

## Discovery vs control traffic

```mermaid
flowchart TB
  Host[Operator PC gateway host or ESP32 with IP config]

  subgraph Outbound_control
    V3["V3 frames<br/>item 0xB000 VMC<br/>item 0xF000 VMA"]
    V4["V4 frames<br/>item 0xB900 VMS"]
  end

  Host -->|UDP listen 53862| SDAP_rx["SDAP packets"]
  SDAP_rx --> Host

  Host -->|TCP connect 53484| SDCP_sock[SDCP session]
  Host -->|UDP datagram 53484| SDCP_udp[VMC broadcast Group/All]
  SDCP_sock --> V3
  SDCP_sock --> V4
  V3 --> Mon[(Monitor)]
  V4 --> Mon
  SDCP_udp --> Mon
```

The **ESP32 native sketch** does not run an SDAP listener; you set `MONITOR_HOST` to the monitor’s IP (often learned once from SDAP on a PC).

## VMC over TCP: C# / web host (reference)

```mermaid
sequenceDiagram
  participant C as Client / Web API
  participant T as TCP SDCP :53484
  participant M as Monitor

  Note over C,M: Read example
  C->>T: V3 item 0xB000 STATget MODEL
  T->>M: forward
  M-->>T: ASCII answer in container
  T-->>C: parse value string

  Note over C,M: Write example
  C->>T: V3 item 0xB000 STATset BRIGHTNESS 512
  T->>M: forward
  M-->>T: response container / status
  T-->>C: optional STAT tokens
```

## VMC over TCP: ESP32 native (`monitor_knobs_sdcp.ino`)

Same wire as above: **13-byte SDCP v3 header** (`version=3`, `category=11`, `SONY`, single-target bytes **6–7 = 0**, request **8 = 0**, item **9–10 = 0xB000** BE, data length **11–12** BE) then **ASCII** (`STATset …` / `STATget …`). The sketch builds this in `buildVmcPacket` / `buildVmcStatSetTail`, sends with `WiFiClient::write`, reads until timeout or buffer full, and treats **`rx[8] == 1`** as response OK (aligned with `SDCP_COMMAND_RESPONSE_OK` in [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs)).

```mermaid
sequenceDiagram
  participant F as ESP32 firmware
  participant W as WiFiClient TCP :53484
  participant M as Monitor

  F->>F: buildVmcPacket STATset token value
  F->>W: connect MONITOR_HOST SDCP_PORT
  F->>W: write 13 + ASCII bytes
  W->>M: SDCP v3 frame
  M-->>W: response bytes
  W-->>F: read loop until idle / cap
  F->>F: validate len, rx[8]==OK
  F->>W: close
```

## ESP32 native: operator modes (runtime)

Short press **MODE** cycles **PICTURE** → **RGB_GAIN** → **GRADE** (see sketch `RunMode` and README table). ADC mapping and NVS keys differ per mode.

```mermaid
stateDiagram-v2
  [*] --> PICTURE
  PICTURE --> RGB_GAIN : MODE btn
  RGB_GAIN --> GRADE : MODE btn
  GRADE --> PICTURE : MODE btn
```

## HTTP path: JSON to monitor (short-lived TCP on server)

```mermaid
flowchart LR
  A["HTTP client JSON"] --> B["POST /api/vmc/set"]
  B --> C[VmcClient STATset]
  C --> D["SdcpConnection :53484"]
  D --> E[(Monitor)]
```

`POST /api/vmc/broadcast` is **UDP** only (`VmcUdpBroadcastClient`); no TCP session to `host`.

## MonitorControl.Web route surface (REST + push)

```mermaid
flowchart TB
  subgraph REST["/api JSON"]
    H[GET /api/health]
    D[GET /api/sdap/discover]
    VG[POST /api/vmc/get]
    VS[POST /api/vmc/set]
    VB[POST /api/vmc/broadcast]
    VI[POST /api/vms/product-info]
    VAv["POST /api/vma/control-software-version<br/>kernel-version rtc<br/>fpga1 fpga2 fpga-core"]
    FW["POST /api/vma/firmware/*<br/>403 unless gate"]
  end

  subgraph Push["Synthetic live data"]
    SSE[GET /api/events/monitor]
    WS[GET /ws/monitor-watch]
  end

  Client[Browser automation MCU] --> REST
  Client --> Push
```

## CLI (`monitorctl`) to transport

| Command | Maps to |
|---------|---------|
| `discover` | `SdapDiscovery` UDP **53862** |
| `vmc` | TCP **53484** `STATget` |
| `vmc-broadcast` | UDP **53484** `VmcUdpBroadcastClient` |
| `vms-info` | TCP **53484** VMS |
| `vma-version` | TCP **53484** VMA read |

```mermaid
flowchart LR
  subgraph Commands
    c1[discover]
    c2[vmc]
    c3[vmc-broadcast]
    c4[vms-info]
    c5[vma-version]
  end

  c1 --> SDAP[SdapDiscovery UDP 53862]
  c2 --> TCP[SdcpConnection TCP 53484]
  c4 --> TCP
  c5 --> TCP
  c3 --> UDP[VmcUdpBroadcastClient UDP 53484]
```

## Physical UI: HTTP gateway vs native SDCP on ESP32

Both examples use **ADC + optional buttons**; only the **last hop** differs.

```mermaid
flowchart TB
  subgraph Sensors[Shared idea pots buttons]
    P1[(Pots)]
    Btn[MODE CAL POWER]
  end

  subgraph PathHttp["examples/arduino-knobs-brightness-contrast"]
    MapH[map ADC to VMC range]
    Http["HTTP POST /api/vmc/set<br/>JSON host args"]
    GW[MonitorControl.Web]
  end

  subgraph PathNative["examples/esp32-sdcp-vmc"]
    MapN[map ADC per mode NVS kbcal]
    Build[buildVmcPacket SDCP v3]
    Tcp[WiFiClient TCP :53484]
  end

  Mon[(Monitor)]

  P1 --> MapH
  Btn --> MapH
  MapH --> Http --> GW --> Mon

  P1 --> MapN
  Btn --> MapN
  MapN --> Build --> Tcp --> Mon
```

## Server-push shape (SSE / WebSocket)

The monitor only speaks **SDCP**. SSE and WebSocket **poll `STATget`** on the server and stream JSON.

```mermaid
flowchart LR
  Mon[(Monitor SDCP)]
  GW[MonitorControl.Web]
  Cli[Browser / automation]

  Mon <--> GW
  GW -->|interval STATget| Mon
  GW -->|SSE or WebSocket JSON| Cli
```

## Long-lived TCP: `Sample.BroadcastControl` REPL

One TCP session for many `get` / `set` lines — spec: [broadcast-realtime-control.md](../spec/broadcast-realtime-control.md).

```mermaid
flowchart LR
  Op[Operator stdin]
  Repl[BroadcastControl REPL]
  Tcp["SdcpConnection<br/>open once"]
  Mon[(Monitor)]

  Op --> Repl
  Repl -->|get set help quit| Tcp
  Tcp <--> Mon
```

## SDK layering (library)

```mermaid
flowchart TB
  App[Your app CLI web host]

  App --> Clients[VmcClient VmsClient VmaClient]
  Clients --> Containers[LegacyVmc LegacyVms LegacyVma]
  Containers --> Buf[SdcpMessageBuffer V3 or V4]
  Buf --> Transports[SdcpConnection TCP<br/>SdcpUdpBroadcastTransport UDP]

  Transports --> Mon[(Monitor LAN)]
```

## Related

- [Engineering handbook](../handbook.md) — trust boundaries, reading order.
- [Web API guide](../guide/web-api-and-python-gateway.md) — bodies, firmware gate, push.
- [spec/broadcast-realtime-control.md](../spec/broadcast-realtime-control.md) — REPL grammar.
- [spec/vmc-string-catalog.md](../spec/vmc-string-catalog.md) — VMC doc index.
- [examples/README.md](../../examples/README.md) — catalog of non-.NET examples.
- [samples/README.md](../../samples/README.md) — .NET samples.
