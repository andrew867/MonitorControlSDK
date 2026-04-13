# Sample: UDP SDCP VMC broadcast

Sends a single **VMC** line over **UDP** to port **53484** with SDCP header set for **All monitors** (`Group ID` / `Unit ID` = `0xFF`), matching the PVM-740 programmer manual **Group / All** UDP mode.

- **Device discovery** (model name, IP, serial) uses **SDAP** on UDP **53862** — see [`Sample.Discovery`](../Sample.Discovery/).
- This sample is for **multi-monitor shading** where the chassis accepts VMC over directed or global broadcast.

Optional first argument: **broadcast IP** (e.g. `192.168.1.255`). Default is `255.255.255.255`.

```bash
dotnet run --project samples/Sample.UdpVmcBroadcast
dotnet run --project samples/Sample.UdpVmcBroadcast -- 192.168.1.255
```

See also: [`VmcUdpBroadcastClient`](../../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs) and [docs/reference/pvm-740-programmer-manual-synthesis.md](../../docs/reference/pvm-740-programmer-manual-synthesis.md).
