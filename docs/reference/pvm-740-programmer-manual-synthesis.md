# PVM-740 Interface Manual for Programmers — synthesis (public excerpt)

This document **consolidates the protocol material** from the HTML excerpt of *PVM-740 Interface Manual for Programmers* hosted on **ManualsLib** (13 pages as rendered), so integrators **do not depend on a third-party site** for core header rules, timing, and VMC spelling.

- **Original manufacturer:** as stated on the published manual excerpt (see link below).  
- **Excerpt consulted:** [ManualsLib — PVM-740 programmer manual](http://www.manualslib.com/manual/1270703/Sony-Pvm-740.html) (pages 3–12 captured 2026-04-13).  
- **Authoritative implementation in this repo:** [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs), [`SdcpConnection`](../../src/MonitorControlSDK/Transport/SdcpConnection.cs), [`SdapAdvertisementPacket`](../../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs), [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs).  
- **Full PVM-740 VMC spellings (appendix):** [appendices/pvm-740-vmc-catalog-from-manual.txt](appendices/pvm-740-vmc-catalog-from-manual.txt).

ManualsLib is a third-party host, not the equipment OEM’s official distribution; if the live page diverges, prefer **captures from your hardware** and update this file.

---

## Notation (from manual)

- Numbers with suffix **`h`** are hexadecimal (`10h` = 16 decimal).  
- **Character strings** use quotation marks (`"SONY"`).

---

## Ethernet / services (manual page 3)

| Service | Port (factory) | Notes (manual) |
|---------|----------------|----------------|
| **SDAP** | **53862** / UDP | Status advertisement. |
| **SDCP** | **53484** / TCP and UDP | Control + status; see connection modes below. |
| **FTP** | **21** | Listed in the manual’s port table; **not implemented** in MonitorControl. |

Cabling: **straight** Ethernet cable; hub should support **AUTO MDIX** (straight/cross detection).

---

## SDAP (manual pages 4–6)

### Role

Monitors/controllers **periodically broadcast** device information on UDP so hosts can **discover** devices.

### Protocol table (manual)

| Field | Manual value |
|-------|----------------|
| Protocol name | SDAP (Simple Display Advertisement Protocol) |
| Transport | **UDP** |
| Port | **53862** |
| Broadcast interval | **Monitor: 15 s**; **Controller: 30 s** |

### Advertised information (manual)

Category; device name; serial number; **location**; community; **power status**.

### SDAP version 4 header (manual page 5)

- Header **4 bytes**: bytes **0–1** = ASCII **`"DA"`** (`44h 41h`); byte **2** = **version `04h`**; byte **3** = **category** — **`0Bh`** monitor, **`0Ch`** monitor **controller**.  
- **Community** (4 bytes): **`"SONY"`** (case-sensitive in SDCP; SDAP uses the same community concept).  
- **Device information** blocks (manual diagram): product name (up to **12** characters, `00h` padded), serial number (hex), connection IP, acceptable IPs (multiple), power status, error (SDAP v3 fields), region/name (v3), **Group ID** and **Unit ID** (v4).

**This repository** decodes a **144-byte** legacy layout in [`SdapAdvertisementPacket`](../../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs) (`"DA"` header, `SONY` at 4–7, product name 8–19, serial 20–23, connection IP **50–53**, group/unit **120–121**). See [spec/sdap-overview.md](../spec/sdap-overview.md) for offset notes vs. the PVM-740 diagram (packing may differ slightly by firmware generation).

### SDAP field glossary (manual page 6 — SDAP ToC fragments)

The excerpt lists additional SDAP payload concepts (versioned across SDAP v2–v4):

| Concept | Manual description (abridged) |
|---------|-------------------------------|
| **Location** | Up to **24** characters; shorter values padded with `00h`. |
| **Connection IP (SDAP v2)** | Host IP the device is connected to for menu control (hex); `00h` = not connected. |
| **Acceptable IP (SDAP v2)** | Up to **four** registered host IPs that may connect via SDCP (hex); `00h` = unused slot. |
| **Error (SDAP v3)** | `0` = no error, `1` = error. |
| **Region / Name (SDAP v3)** | Up to 24 characters, `00h` padded. |
| **Group ID / Unit ID (SDAP v4)** | 1-byte hex IDs for grouping. |

---

## SDCP (manual pages 4–7)

### Role

**SDCP** carries monitor **commands and status**. VMC packets are **superimposed** in the SDCP **Data** field (manual).

### Protocol table (manual)

| Field | Manual value |
|-------|----------------|
| Name | SDCP (Simple Display Control Protocol) |
| Transport | **TCP and UDP** |
| Port | **53484** |
| TCP connection timeout | **30 seconds** (manual table) |

### Connection modes (manual)

| Mode | TCP vs UDP | Purpose (manual) |
|------|------------|-------------------|
| **Single** | TCP | One monitor. |
| **Peer to Peer** | TCP | One monitor. |
| **Group** | **UDP broadcast** | Multiple monitors; **Group ID** 1–99. |
| **All** | **UDP broadcast** | **Group ID = Unit ID = `FFh`**. |

**Manual rule:** status from the monitor is available on **TCP**; **do not** rely on status when driving the device **only via UDP** broadcast.

**Manual rule:** when a **Group ID** is set for UDP broadcast, **monitors do not return** a response.

**MonitorControl** implements **TCP** `SdcpConnection` to **53484**, **SDAP** listen on **53862**, and **UDP SDCP VMC** send helpers (`SdcpUdpBroadcastTransport`, `VmcUdpBroadcastClient`) for manual **Group / All** shading. **SDAP** remains the advertisement / neighbor-discovery path; UDP SDCP does not return status in those broadcast modes.

### Command pacing (manual pages 4–5)

1. **Do not** send the next SDCP command until **return data** for the previous command was received — otherwise the monitor may not accept the next command or return an error.  
2. **Wait time** (manual): host receives return data **4–8 ms** after issuing a packet on **10BASE-T**, assuming no failure.  
3. For **UDP** menu/cursor/knob bursts: leave **≥ 50 ms** between commands (BKM-15R reference: **100 ms**); long operations (e.g. white balance) need longer gaps; excess commands may be **queued** then **dropped** when the queue is full.  
4. On communication error, the monitor **discards** partial input and waits for a **new** command.  
5. **Response-oriented behavior** (manual): for some malformed **Data** areas, the monitor may still return **OK** quickly while processing continues; malformed **non-data** areas get an appropriate error packet. Status requests may return data even for **non-existent** fields (manual caution).

### SDCP version 3 packet — header (manual page 6)

Layout described in the excerpt:

| Region | Size | Content |
|--------|------|---------|
| **Header** | 2 bytes | Byte **0**: **version `03h`**. Byte **1**: **category `0Bh`** for the monitor; other values are **ignored/rejected**. |
| **Community** | 4 bytes | Case-sensitive ASCII **`"SONY"`**; must be **exactly** four characters. |
| **Group ID** | 1 byte | `0` = Single / P2P; **`FFh`** = All; **1–99** = Group (manual cites BKM-15R 7-seg range). UDP broadcast: **no TCP response**. |
| **Unit ID** | 1 byte | `0` = P2P or Group; **`FFh`** = All; otherwise must **match** the monitor when using **TCP**. |

When SDAP shows a non-zero **Unit ID** (for example group/unit `1`/`1`) and TCP commands stall after a few exchanges, set **single-connection** addressing (`group` = `0`, `unit` = advertised unit) via `VmcClient.TcpSingleUnitId` / REST `sdcpUnitId` / CLI `--sdcp-unit`.

Then the manual’s **Command** block continues with **Request/response**, **Item number**, **Data length**, **Data** — matching this repo’s **13-byte** prefix before payload:

| Offset | Field | Manual / repo |
|--------|--------|----------------|
| 8 | Request / response | **Request = `00h`** (manual labels this “SET” request). **OK = `01h`**, **NG = `00h`** for response (`SdcpMessageBuffer.SDCP_COMMAND_*`). |
| 9–10 | Item number (BE) | **`B000h`** — “monitor command” (VMC ASCII). **`B001h`** — “monitor command for monitors with **built-in controllers**” (manual). Constants: `SdcpMessageBuffer.SdcpV3ItemVideoMonitorControl` / `SdcpV3ItemVideoMonitorControlBuiltIn`. |
| 11–12 | Data length (BE) | Length **n** of **Data**; manual maximum **499** (`01F3h`). |

**Buffer size:** the manual’s excerpt focuses on the **used** fields; this repo allocates **960-byte** `packetData` and **973-byte** max read for compatibility with legacy tools (see [sdcp-framing-and-items.md](sdcp-framing-and-items.md)).

### Error response (manual page 7)

On error: response **NG**, **Item number** repeats the **request’s** item, and an **error code** follows (manual: **1-byte category** + **1-byte error**). Cross-check numeric catalog: [sdcp-error-codes.md](sdcp-error-codes.md). The excerpt mentions **communication** class errors (e.g. checksum / `F0**h` family) — align with your capture.

---

## VMC (manual pages 8–12)

### Payload format (manual)

- VMC lives in the SDCP **Data** field for item **`B000h`**.  
- Strings are **space-separated**: `"Category Command Parameter1 …"`.  
- **No `0x00` terminator** is sent on the wire.  
- **Do not** append a **trailing space** after the last token (manual: may confuse receivers).

### Categories named in the manual (page 9)

| Category | Direction | Meaning |
|----------|------------|---------|
| `STATset` | Controller → monitor | Set status. |
| `STATget` | Controller → monitor | Request status. |
| `STATret` | Monitor → controller | Response to `STATget` (manual naming). |
| `INFOknob` | Controller → monitor | Rotary switch state (manual). |
| `INFObutton` | Controller → monitor | Keypad / cursor buttons (manual). |

**MonitorControl** ships **`STATget` / `STATset`** via [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs). It does **not** include dedicated helpers for **`INFOknob` / `INFObutton`** — you can still send them with the same client by using the appropriate **category string** as the first `Send(…)` segment if your device accepts them.

### TCP sequence (manual page 8)

- Monitor returns a **response on reception**.  
- Long-running commands: execution may **continue after** the response.  
- If a **new command** arrives while one runs, it may be **queued**; **status** (`STATget`) returns when the value is **ready**.

### Full PVM-740 command spellings

See **[appendices/pvm-740-vmc-catalog-from-manual.txt](appendices/pvm-740-vmc-catalog-from-manual.txt)** (pages 9–12). For tokens also listed in the broader reference corpus, cross-check [appendices/vmc-stat-tokens-from-references.txt](appendices/vmc-stat-tokens-from-references.txt).

---

## Implementation checklist (this repository)

| Manual requirement | Repo status |
|---------------------|-------------|
| SDCP TCP **53484** | `SdcpConnection.DefaultPort` |
| SDCP UDP **53484** (Group / All VMC) | `SdcpUdpBroadcastTransport` + `VmcUdpBroadcastClient` |
| SDAP UDP **53862** | `SdapDiscovery.DefaultPort` |
| V3 header **03h / 0Bh / SONY** | `setupVmcPacketHeader` / `setupVma` |
| Item **B000h** VMC | `VmcClient` default (`VmcItemNumber` / `setupVmcPacketHeader()`) |
| Item **B001h** VMC (built-in controller) | `VmcClient.VmcItemNumber`, `VmcUdpBroadcastClient.VmcItemNumber`, `SdcpMessageBuffer.ParseVmcItemSpecifier`, REST `vmcItem`, CLI `--vmc-item`, web UI selector |
| Max VMC data **499** (manual) | `SDCP_VMC_MAX_DATA_LEN = 499` in `SdcpMessageBuffer` |
| Request/response bytes | `SDCP_COMMAND_REQUEST`, `…_RESPONSE_OK`, `…_RESPONSE_NG` |
| Group / unit targeting | `setSingleConnection`, `setGroupConnection`, `setAllConnection`, `setP2pConnection` |
| FTP / full firmware OTA UI | **Out of scope** — see [guide/firmware-updates.md](../guide/firmware-updates.md) for VMA service writes. |

---

## Related docs

- [sdcp-framing-and-items.md](sdcp-framing-and-items.md) — V4 / VMS, buffer sizes, item list.  
- [sdcp-error-codes.md](sdcp-error-codes.md) — numeric error catalog.  
- [external-sources.md](external-sources.md) — ManualsLib URL + community libraries.  
- [references-parity.md](references-parity.md) — reference trees vs shipped SDK.
