# MonitorControl.Sdk

**Version 0.4.0** — NuGet package id **`MonitorControl.Sdk`**; .NET namespaces **`MonitorControl.*`**.

.NET 8 library for **SDAP** (UDP discovery on **53862**), **SDCP** on **TCP 53484** (`SdcpConnection`, `VmcClient`, `VmsClient`, …), and optional **UDP SDCP VMC** on **53484** for **Group / All** broadcast (`VmcUdpBroadcastClient`) per the PVM-740 programmer excerpt.

**Docs:** [docs/index.md](../../docs/index.md) · **Handbook:** [docs/handbook.md](../../docs/handbook.md) · **Samples:** [samples/README.md](../../samples/README.md) · **Repo:** [github.com/andrew867/MonitorControlSDK](https://github.com/andrew867/MonitorControlSDK)

**0.2.0** renamed the public package and root namespace to vendor-neutral **`MonitorControl.Sdk`** / **`MonitorControl.*`** (**0.1.x** used the previous package id and namespaces). **0.3.0** added the handbook, committed OpenAPI, and initial diagram refresh. **0.4.0** deepens diagrams and cross-docs (ESP32 on-wire, Python proxy, sample accuracy); wire behavior is unchanged from 0.3.0.
