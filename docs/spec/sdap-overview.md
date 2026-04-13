# SDAP overview (short)

**SDAP** — UDP **53862**, advertisement layout decoded by [`SdapAdvertisementPacket`](../../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs). Listening helper: [`SdapDiscovery`](../../src/MonitorControlSDK/Transport/SdapDiscovery.cs).

Field offsets (product name, serial, IP octets 50–53, group/unit 120–121, `SONY` community at 4–7) are documented inline on the type.

## PVM-740 programmer manual (excerpt) alignment

The **ManualsLib** excerpt of *PVM-740 Interface Manual for Programmers* describes **SDAP v4**: header ASCII **`"DA"`** (`44h 41h`), version **`04h`**, category **`0Bh`** (monitor) or **`0Ch`** (monitor controller), community **`"SONY"`**, plus product name (12 chars), serial, connection IP, acceptable IPs, power status, optional error/region/name fields, and **Group ID / Unit ID** (see [reference/pvm-740-programmer-manual-synthesis.md](../reference/pvm-740-programmer-manual-synthesis.md)).

Operational notes from that excerpt:

- Default **broadcast interval**: monitor **15 s**, controller **30 s** (your firmware may differ).  
- When driving **UDP** menu/cursor traffic toward devices, leave **≥ 50 ms** between packets (BKM-15R reference **100 ms** in the excerpt).

Optional third-party cross-checks: [reference/external-sources.md](../reference/external-sources.md) (not required for implementation in this repo).
