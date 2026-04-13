# Sony.MonitorControl

**Version 0.1.1** — Semver from this line forward.

.NET 8 library for **SDAP** (UDP discovery on **53862**), **SDCP** on **TCP 53484** (`SdcpConnection`, `VmcClient`, `VmsClient`, …), and optional **UDP SDCP VMC** on **53484** for **Group / All** broadcast (`VmcUdpBroadcastClient`) per the PVM-740 programmer excerpt.

**Docs:** [docs/index.md](../../docs/index.md) · **Samples:** [samples/](../../samples/) · **Repo:** [github.com/andrew867/MonitorControlSDK](https://github.com/andrew867/MonitorControlSDK)

Patch **0.1.1** fixes endian helper code so modern SDKs resolve `Reverse()` unambiguously (CI-safe). Enjoy fewer surprises and more time actually controlling monitors.
