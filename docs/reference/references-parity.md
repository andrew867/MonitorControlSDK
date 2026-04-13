# References tree ↔ shipped implementation (parity)

The [`references/`](../../references/) directory holds **decompiled / extracted Sony-adjacent tooling** (C# monitor apps and related C assets) used to **verify wire behavior** while building **Sony.MonitorControl**. This page is the **single checklist** mapping every significant reference subtree to:

- what the **new stack** implements (`src/`, `samples/`, `src/MonitorControl.Web/`), and  
- what remains **out of scope** (WinForms UI, measurement hardware DLLs, or undocumented firmware-only paths).

Operators and programmers should **not** need to spelunk `references/` once they have this document plus the generated appendices.

## Public Sony programmer manual (PVM-740 excerpt)

The **ManualsLib** HTML excerpt of *Sony PVM-740 Interface Manual for Programmers* is **re-stated in full** (protocol tables, timing, SDAP/SDCP header, VMC categories, and a verbatim VMC command list) so operators are not dependent on that third-party host: [pvm-740-programmer-manual-synthesis.md](pvm-740-programmer-manual-synthesis.md).

## Machine-generated inventories (always use these first)

| Artifact | Contents | Regenerate |
|----------|-----------|------------|
| [appendices/vms-opcode-constants.txt](appendices/vms-opcode-constants.txt) | Every `CMD_*` opcode constant in `LegacyVmsContainer` | `bash scripts/regen-appendices.sh` |
| [appendices/vms-engine-send-methods.txt](appendices/vms-engine-send-methods.txt) | Every `VmsCommandEngine.send*` entry point | same |
| [appendices/vmc-stat-tokens-from-references.txt](appendices/vmc-stat-tokens-from-references.txt) | **Exhaustive literal** `STATget` / `STATset` / broadcast strings found under `references/**/*.cs` | same |

Human-oriented summaries: [vms-overview.md](vms-overview.md), [vmc-command-surface.md](vmc-command-surface.md), [vma-wire-reference.md](vma-wire-reference.md), [sdcp-framing-and-items.md](sdcp-framing-and-items.md).

## Parity matrix (C# protocol stacks)

| Reference path | Role in original tooling | Shipped equivalent |
|----------------|---------------------------|-------------------|
| `references/Monitor_Update/MonitorNetwork/MonitorNetwork/*.cs` | Core **SDAP**, **SDCP** framing, **VMC/VMS/VMA** containers and commands | [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs), [`SdapAdvertisementPacket`](../../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs), [`LegacyVmcContainer`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs), [`LegacyVmsContainer`](../../src/MonitorControlSDK/Internal/LegacyVmsContainer.cs), [`LegacyVmaContainer`](../../src/MonitorControlSDK/Internal/LegacyVmaContainer.cs), [`SdcpConnection`](../../src/MonitorControlSDK/Transport/SdcpConnection.cs), [`SdapDiscovery`](../../src/MonitorControlSDK/Transport/SdapDiscovery.cs), clients in [`Clients/`](../../src/MonitorControlSDK/Clients/) |
| `references/MonitorNetwork/MonitorNetwork/*.cs` | Older / parallel **MonitorNetwork** assembly (same class names) | Same SDK types (single canonical port in `src/`; references kept for diff only — see [plan/00-inventory.md](../plan/00-inventory.md)) |
| `references/Monitor_Update/VerUpTool/*.cs` | Firmware updater UI; **ControlVmcCommand**, **ControlVmsCommand** | **VMC/VMS wire:** `VmcClient`, `VmsCommandEngine`. **Firmware wizard / chunk file I/O / UI:** *not ported* — use [guide/firmware-updates.md](../guide/firmware-updates.md) + guarded HTTP VMA routes + your own binary pipeline |
| `references/LMD_AutoWhiteBalance/DaiginjoSdcp/*.cs` | LMD fork of SDCP/SDAP/VMC/VMA stack | Same SDK transport + containers |
| `references/LMD_AutoWhiteBalance/SMAutoWB/*.cs` | Auto white-balance **WinForms** + probe drivers + long `STATset` sequences | **Wire sequences** are reflected in [appendices/vmc-stat-tokens-from-references.txt](appendices/vmc-stat-tokens-from-references.txt). **UI, i1/CA210 DLLs, color math:** not in this repo |
| `references/Monitor_AutoWhiteAdjustment/BvmAutoWhiteBalanceTool/*.cs` | BVM auto-WB tool + **ControlVmcCommand** + broadcast helpers | **VMC** via `VmcClient`. **`sendCommandBroadCast`**: not implemented on TCP client — see appendix notes; use unicast `STATset` if the device accepts it |

## VMC gaps vs legacy helpers (documented, not hidden)

| Legacy API (references) | Status in Sony.MonitorControl |
|---------------------------|-------------------------------|
| `VmcCommand.sendCommandBroadCast("STATset", …)` | **Not ported.** Original pattern targeted broadcast discovery flows. Replicate with normal TCP `VmcClient.Send("STATset", …)` when the chassis supports the same tokens unicast. |
| `STATset` with empty string tail (`""`) | Observed in VerUpTool; **no dedicated helper**. You can `Send("STATset")` with no further segments only if you intentionally need that vendor quirk. |
| `STATret` constant | Present in sources as `VMC_CMM_RET`; **no literal `sendCommand("STATret", …)`** in `references/**/*.cs` scan. Treat as **reserved / unknown** unless you validate on hardware. |
| `MODEL2`, `BACKLIGHT` as `VMC_CMM_*` identifiers | **`MODEL2`**: constant only in BVM `ControlVmcCommand`; not seen as a `STATget` literal in the scan — try `GetStatString("MODEL2")` on hardware if needed. **`BACKLIGHT`**: appears in **STATget** in BVM tool — included in appendix. |

## VMS / VMA

- **VMS:** Full opcode surface and send-matrix are the two VMS appendices; `VmsCommandEngine` is the programmatic surface. `VerUpTool`’s `ControlVmsCommand` is a thin wrapper over the same **`VmsCommand`** patterns (product info, common control start, etc.) — no second hidden opcode set.
- **VMA (service / firmware):** `LegacyVmaContainer` + `VmaClient` match `VmaContainer` / `VmaServiceCommand` lineage. **This repository does not ship a validated OTA state machine** (packing, retries, service mode entry). Read [guide/firmware-updates.md](../guide/firmware-updates.md) before any write.

## `references/MSSONY/` (C decompilation)

Files such as `controler_sdcp.vxe.c`, `product_*.so.c`, are **firmware / controller binaries** reverse-engineered to C. They may contain **additional** protocol hints or string tables not present in the C# tools.

- **Not required** to use Sony.MonitorControl.
- **Not auto-mined** into appendices (different language, noisy). If you discover a **new** ASCII token there and confirm it on hardware, add it to the next regeneration notes or open a PR updating the VMC docs.

## Keeping this page honest after edits

1. When you change **`LegacyVmsContainer`** or **`VmsCommandEngine`**, run `bash scripts/regen-appendices.sh` so VMS appendices stay exhaustive.
2. When you add **new C# reference** trees under `references/`, re-run the same script so **VMC** literals stay exhaustive.
3. Update this **parity matrix** if a new subtree appears (service app, new fork).

## Legal

`references/` material is for interoperability research inside this repository. Distribution of decompiled sources may be restricted in your jurisdiction; the **clean-room** public API is **Sony.MonitorControl** under `src/` plus the docs under `docs/`.
