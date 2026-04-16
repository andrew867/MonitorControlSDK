# VMC command surface (`STATget` / `STATset`)

VMC uses SDCP **V3** with an ASCII payload: `CATEGORY arg1 arg2 …` packed into the data area — see [`LegacyVmcContainer.setCommand`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs) and [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs) (**TCP** session, request/response). The SDCP item is **`0xB000`** by default, or **`0xB001`** for monitors that use the built-in-controller item (set `VmcClient.VmcItemNumber` / REST `vmcItem` / CLI `--vmc-item`; see [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs)). The **same** payload layout can be sent on **UDP** port **53484** for manual **Group / All** broadcast modes using [`VmcUdpBroadcastClient`](../../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs) (fire-and-forget; no response per PVM-740 excerpt).

**Non-.NET on-wire example:** [`examples/esp32-sdcp-vmc/monitor_knobs_sdcp.ino`](../../examples/esp32-sdcp-vmc/monitor_knobs_sdcp.ino) builds this V3 + **0xB000** header and ASCII body in firmware (validated against the layout in [sdcp-framing-and-items.md](sdcp-framing-and-items.md)). Diagrams: [diagrams/monitor-control-flows.md](../diagrams/monitor-control-flows.md).

## Categories

| Category | Direction | Example |
|-----------|-----------|---------|
| `STATget` | Host → monitor → ASCII answer | `STATget MODEL` |
| `STATset` | Host → monitor → status in response container | `STATset BRIGHTNESS 512` |
| `STATret` | Declared in legacy `ControlVmcCommand` as `VMC_CMM_RET` | No `sendCommand("STATret", …)` literal found in the reference C# corpus scan; treat as **reserved** until validated on hardware. |

## PVM-740 public manual (model-specific VMC)

The **PVM-740 Interface Manual for Programmers** excerpt (ManualsLib) lists many additional **`STATset` / `INFObutton`** lines (input select, markers, scan, languages, …). Those spellings are copied verbatim (plus examples for numeric parameters) in:

**[appendices/pvm-740-vmc-catalog-from-manual.txt](appendices/pvm-740-vmc-catalog-from-manual.txt)**

Narrative + timing + SDAP/SDCP tables: [pvm-740-programmer-manual-synthesis.md](pvm-740-programmer-manual-synthesis.md).

## Exhaustive literal catalog (reference corpus)

All **string literals** collected from the C# trees under `references/` are kept in one machine-regenerated file (no manual `rg` needed in the field):

**[appendices/vmc-stat-tokens-from-references.txt](appendices/vmc-stat-tokens-from-references.txt)** — sections:

1. **STATget** / `getSTATgetMessage("…")` field names  
2. **STATset** first token (`sendCommand` second argument); more tokens may follow  
3. **`sendCommandBroadCast("STATset", "…")`** strings (legacy UDP broadcast path — use [`VmcUdpBroadcastClient`](../../src/MonitorControlSDK/Clients/VmcUdpBroadcastClient.cs); see [references-parity.md](references-parity.md))

Regenerate after adding new C# trees under `references/`:

```bash
bash scripts/regen-appendices.sh
```

## Tokens table (human index; non-exhaustive)

The appendix file above is authoritative for **literals**. This table groups the same tokens by **meaning** for quick reading.

| Token / pattern | Typical meaning |
|-----------------|-----------------|
| `RGAIN`, `GGAIN`, `BGAIN` | RGB gain |
| `RBIAS`, `GBIAS`, `BBIAS` | RGB bias |
| `CONTRAST`, `BRIGHTNESS` | Picture |
| `ALLCONTRAST`, `ALLBRIGHTNESS` | Global picture |
| `FLATFIELDPATTERN` + `ON` / `OFF` (or single string `FLATFIELDPATTERN ON`) | Flat field |
| `WBSEL USER` (or `WBSEL`, `USER`) | User white balance |
| `COLORR`, `COLORG`, `COLORB`, `COLORW` + value | Chroma / white drive |
| `MODEL`, `SHOWID`, `SHOWIPADDRESS`, `MENUOFF`, `ENTER` | Identity / UI helpers |
| `SHOWBOTHIDIP`, `SHOWADDR SINGLE` | ID / IP display variants (also used with broadcast in legacy tools) |
| `BACKLIGHT` | Appears as **STATget** in BVM auto-WB reference |
| `MODEL2` | Constant in BVM `ControlVmcCommand`; **not** in literal scan — probe with `STATget MODEL2` if your chassis supports it |

**Support is model-specific** for every token. Unknown tokens: send and inspect the SDCP/VMC response / NAK.

## SDK usage

```csharp
vmc.GetStatString("MODEL");
vmc.Send("STATset", "BRIGHTNESS", "512");
```

## Parity and scope

- Full **references ↔ SDK** map, broadcast gaps, firmware scope: [**references-parity.md**](references-parity.md).  
- **Firmware / VMA writes** are wire-accurate but **not** a complete validated OTA product: [guide/firmware-updates.md](../guide/firmware-updates.md).  
- Visual overview: [diagrams/monitor-control-flows.md](../diagrams/monitor-control-flows.md).
