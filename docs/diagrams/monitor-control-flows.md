# Monitor control flows (Mermaid)

High-level views of **where data moves** and **how common picture controls** reach the monitor. Ports and item numbers match [SDCP framing and item numbers](../reference/sdcp-framing-and-items.md). **HTTP routes** match [`MonitorApiExtensions`](../../src/MonitorControl.Web/MonitorApiExtensions.cs) + [`MonitorPushEndpoints`](../../src/MonitorControl.Web/MonitorPushEndpoints.cs) as of the committed [OpenAPI snapshot](../../openapi/monitorcontrol.openapi.json).

## End-to-end stacks

Two common integration shapes: **direct SDCP** from a PC or embedded device that speaks the wire protocol, or **HTTP JSON** through [MonitorControl.Web](../../src/MonitorControl.Web/) (same SDCP underneath). The CLI (`monitorctl`) uses the **SDK directly** (no HTTP).

```mermaid
flowchart LR
  subgraph Clients
    Web[Browser / SPA]
    Py[Python / scripts]
    DotNet[.NET CLI / SDK]
    Mcu[ESP32 / ESP8266 HTTP]
  end

  subgraph Gateway_optional["Gateway (optional)"]
    WebApi["MonitorControl.Web :5080"]
    PyGw["python-service proxy"]
  end

  subgraph Monitor_LAN["Monitor (LAN)"]
    SDAP["UDP SDAP :53862<br/>advertisements"]
    SDCP["TCP SDCP :53484<br/>VMC / VMS / VMA"]
    SDCPu["UDP SDCP :53484<br/>VMC Group/All"]
    Mon[(Professional monitor)]
  end

  Web --> WebApi
  Py --> PyGw
  PyGw --> WebApi
  Mcu --> WebApi

  WebApi --> SDCP
  DotNet --> SDCP
  DotNet --> SDCPu
  Cli[monitorctl CLI] --> SDCP
  Cli --> SDCPu
  Cli --> SDAP

  SDAP -.->|discover IP / serial| Web
  SDAP -.->|discover| Py
  SDAP -.->|discover| DotNet
  SDAP -.->|discover| Cli
  Mon --> SDAP
  Mon <--> SDCP
  SDCPu --> Mon
```

## Discovery vs control traffic

```mermaid
flowchart TB
  Host[Operator PC or gateway host]

  subgraph Outbound_control
    V3["V3 frames<br/>item 0xB000 VMC<br/>item 0xF000 VMA"]
    V4["V4 frames<br/>item 0xB900 VMS"]
  end

  Host -->|UDP listen 53862| SDAP_rx["SDAP packets<br/>product / IP / serial"]
  SDAP_rx --> Host

  Host -->|TCP connect 53484| SDCP_sock[SDCP session]
  Host -->|UDP datagram 53484| SDCP_udp[VMC broadcast<br/>Group / All]
  SDCP_sock --> V3
  SDCP_sock --> V4
  V3 --> Mon[(Monitor)]
  V4 --> Mon
  SDCP_udp --> Mon
```

## VMC: reads and writes (picture and identity)

`STATget` returns ASCII; `STATset` carries tokens such as `BRIGHTNESS`, `CONTRAST`, `MODEL` (model-dependent). See [VMC command surface](../reference/vmc-command-surface.md).

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

## HTTP path: browser or MCU to monitor

The web host opens a **short-lived SDCP connection** per request for most `/api/*` routes (VMC get/set, VMS, VMA), then closes TCP. **UDP VMC broadcast** uses `VmcUdpBroadcastClient` without a TCP session.

```mermaid
flowchart LR
  A["HTTP client<br/>JSON body"] --> B["POST /api/vmc/set<br/>host + args array"]
  B --> C[VmcClient.Send STATset]
  C --> D["SdcpConnection<br/>53484"]
  D --> E[(Monitor)]
```

## MonitorControl.Web route surface (REST + push)

```mermaid
flowchart TB
  subgraph REST["/api — JSON request/response"]
    H[GET /api/health]
    D[GET /api/sdap/discover]
    VG[POST /api/vmc/get]
    VS[POST /api/vmc/set]
    VB[POST /api/vmc/broadcast]
    VI[POST /api/vms/product-info]
    VAv[VMA reads: control-software-version, kernel-version, rtc, fpga*]
    FW[VMA firmware: upgrade-* + restart<br/>403 unless firmware gate]
  end

  subgraph Push["Synthetic live data"]
    SSE[GET /api/events/monitor<br/>SSE text/event-stream]
    WS[GET /ws/monitor-watch<br/>WebSocket JSON snapshots]
  end

  Client[Browser / automation / MCU] --> REST
  Client --> Push
```

## CLI (`monitorctl`) to transport

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

## Local controls: knobs to brightness / contrast

Physical pots on ADC pins → firmware maps to numeric range → **HTTP** to the gateway (recommended on MCUs) or raw SDCP if you port the framing.

```mermaid
flowchart LR
  subgraph Desk
    P1[(Brightness pot)]
    P2[(Contrast pot)]
    ADC[MCU ADC reads]
  end

  P1 --> ADC
  P2 --> ADC
  ADC --> Map[map to 0..1023\nor chassis range]
  Map --> HTTP["POST .../api/vmc/set<br/>BRIGHTNESS / CONTRAST"]
  HTTP --> GW["MonitorControl.Web<br/>on LAN PC or SBC"]
  GW --> SDCP[TCP SDCP]
  SDCP --> Mon[(Monitor)]
```

## Server-push shape (SSE / WebSocket)

The monitor speaks **SDCP request/response** only. “Push” in `MonitorControl.Web` is implemented by **server-side polling** of `STATget` and streaming JSON to browsers or tools.

```mermaid
flowchart LR
  Mon[(Monitor SDCP)]
  GW[MonitorControl.Web]
  Cli[Browser / automation]

  Mon <--> GW
  GW -->|interval STATget| Mon
  GW -->|SSE or WebSocket JSON| Cli
```

## SDK layering (library only)

```mermaid
flowchart TB
  App[Your app / CLI / web host]

  App --> Clients[VmcClient / VmsClient / VmaClient]
  Clients --> Containers[LegacyVmc / LegacyVms / LegacyVma containers]
  Containers --> Buf[SdcpMessageBuffer V3 or V4]
  Buf --> Transports[Tcp SdcpConnection<br/>Udp SdcpUdpBroadcastTransport]

  Transports --> Mon[(Monitor LAN)]
```

## Related

- [Engineering handbook](../handbook.md) — full reading order and trust boundaries.
- [Web API guide](../guide/web-api-and-python-gateway.md) — route table, SSE/WebSocket, firmware gate.
- [OpenAPI codegen](../guide/openapi-codegen.md) — committed `openapi/*.json`, generate C client.
- HTTP knobs: [`examples/arduino-knobs-brightness-contrast/`](../../examples/arduino-knobs-brightness-contrast/).
- Native SDCP knobs: [`examples/esp32-sdcp-vmc/`](../../examples/esp32-sdcp-vmc/).
