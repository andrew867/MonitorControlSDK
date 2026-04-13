# Quick start (~10 minutes)

You need **.NET 8 SDK** ([download](https://dotnet.microsoft.com/download/dotnet/8.0)). This guide uses a monitor on your LAN with **Ethernet enabled** and **SDCP allowed** (wording varies by model).

## 1. Clone and build (2 min)

```bash
git clone https://github.com/andrew867/MonitorControlSDK.git
cd MonitorControlSDK
dotnet build MonitorControl.sln -c Release
dotnet test MonitorControl.sln -c Release
```

## 2. Discover monitors with SDAP (2 min)

SDAP is **UDP port 53862** — devices broadcast a small packet you can listen for:

```bash
dotnet run --project src/MonitorControl.Cli -- discover
```

Note the **IP address** printed for each unit (see [`SdapAdvertisementPacket.ConnectionIp`](../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs)).

## 3. First SDCP command — read a VMC field (3 min)

SDCP uses **TCP port 53484** by default ([`SdcpConnection.DefaultPort`](../src/MonitorControlSDK/Transport/SdcpConnection.cs)):

```bash
dotnet run --project src/MonitorControl.Cli -- vmc --host 192.168.0.10 MODEL
```

Or the minimal sample:

```bash
dotnet run --project samples/Sample.Vmc -- 192.168.0.10 MODEL
```

### Optional: UDP SDCP VMC (multi-monitor “Group / All”)

**SDAP** on UDP **53862** is still how monitors **advertise** themselves. **UDP SDCP** on **53484** is for **broadcast shading** when your fleet is configured for Group/All (see [pvm-740-programmer-manual-synthesis.md](reference/pvm-740-programmer-manual-synthesis.md)):

```bash
dotnet run --project src/MonitorControl.Cli -- vmc-broadcast --scope all -- STATset BRIGHTNESS 512
dotnet run --project samples/Sample.UdpVmcBroadcast -- 192.168.1.255
```

## 4. Use the library from C# (3 min)

```csharp
using MonitorControl.Clients;
using MonitorControl.Transport;

using var tcp = new SdcpConnection("192.168.0.10");
tcp.Open();
var vmc = new VmcClient(tcp);
Console.WriteLine(vmc.GetStatString("MODEL"));
```

## 5. HTTP API (web or any language)

```bash
dotnet run --project src/MonitorControl.Web --urls http://127.0.0.1:5080
```

Open `http://127.0.0.1:5080/` for the browser demo and `http://127.0.0.1:5080/swagger` for OpenAPI. Details: [guide/web-api-and-python-gateway.md](guide/web-api-and-python-gateway.md).

Optional **Python** reverse proxy on port **8000**: [examples/python-service/README.md](../examples/python-service/README.md) (proxies `/api/*` and SSE; **WebSocket** still uses the .NET port).

## 5b. ESP32 native SDCP (no HTTP, no PC in the control path)

If you need **on-wire** VMC from an ESP32 on the same LAN as the monitor, open the **esp32-sdcp-vmc** folder in Arduino IDE (sketch `monitor_knobs_sdcp.ino` plus `wifi_sdap_web.ino` and `config_portal.h`). First boot starts a **captive portal** for WiFi; use the page or **SDAP** discovery to set the monitor IP (NVS `mhost`), then use pots/buttons per [examples/esp32-sdcp-vmc/README.md](../examples/esp32-sdcp-vmc/README.md). Flow diagrams: [diagrams/monitor-control-flows.md](diagrams/monitor-control-flows.md).

## 6. Go deeper

| Goal | Read next |
|------|-----------|
| Full-stack narrative (protocols + code + examples) | [handbook.md](handbook.md) |
| All error codes | [reference/sdcp-error-codes.md](reference/sdcp-error-codes.md) |
| V3 vs V4 headers, item numbers | [reference/sdcp-framing-and-items.md](reference/sdcp-framing-and-items.md) |
| VMC `STATget` / `STATset` tokens | [reference/vmc-command-surface.md](reference/vmc-command-surface.md) |
| VMS structured commands | [reference/vms-overview.md](reference/vms-overview.md) |
| Firmware / VMA | [guide/firmware-updates.md](guide/firmware-updates.md) |

## Troubleshooting

- **Timeout / connection refused:** firewall, wrong IP, SDCP disabled on monitor, or incompatible / unexpected firmware path.
- **Empty or `(null)` VMC:** field name not supported on that model, or password lock (see error codes doc).
