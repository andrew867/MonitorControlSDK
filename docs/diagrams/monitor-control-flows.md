# Monitor control flows (Mermaid)

High-level views of **where data moves** and **how common picture controls** reach the monitor. Ports and item numbers match [SDCP framing and item numbers](../reference/sdcp-framing-and-items.md).

## End-to-end stacks

Two common integration shapes: **direct SDCP** from a PC or embedded device that speaks the wire protocol, or **HTTP JSON** through [MonitorControl.Web](../../src/MonitorControl.Web/) (same SDCP underneath).

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

  SDAP -.->|discover IP / serial| Web
  SDAP -.->|discover| Py
  SDAP -.->|discover| DotNet
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

The web host opens a **short-lived SDCP connection** per request, runs `VmcClient`, then closes TCP.

```mermaid
flowchart LR
  A["HTTP client<br/>JSON body"] --> B["POST /api/vmc/set<br/>host + args array"]
  B --> C[VmcClient.Send STATset]
  C --> D["SdcpConnection<br/>53484"]
  D --> E[(Monitor)]
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

## Related

- [Web API guide](../guide/web-api-and-python-gateway.md) — route table, SSE/WebSocket, firmware gate.
- [OpenAPI codegen](../guide/openapi-codegen.md) — fetch `swagger.json`, generate C client.
- HTTP knobs: [`examples/arduino-knobs-brightness-contrast/`](../../examples/arduino-knobs-brightness-contrast/).
- Native SDCP knobs: [`examples/esp32-sdcp-vmc/`](../../examples/esp32-sdcp-vmc/).
