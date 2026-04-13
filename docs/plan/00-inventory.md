# Legacy code inventory and canonical baseline

## Canonical baseline

**Primary source:** [MonitorNetwork/MonitorNetwork/](MonitorNetwork/MonitorNetwork/) (SDK-style `net48` project).

**Rationale:** This tree is complete (includes `VmsCommand.cs`). [Monitor_Update/MonitorNetwork/MonitorNetwork/](Monitor_Update/MonitorNetwork/MonitorNetwork/) differs only cosmetically in several files (field order, minor edits). [LMD_AutoWhiteBalance/DaiginjoSdcp/](LMD_AutoWhiteBalance/DaiginjoSdcp/) is an older embedded subset: it **does not** contain `VmsCommand.cs` (LMD Auto WB uses VMC-only paths for most SDCP control in that app).

## File-by-file diff summary

### MonitorNetwork vs Monitor_Update/MonitorNetwork

Files reported different by `diff -rq` (substantive review: mostly trivial):

| File | Notes |
|------|--------|
| `SdapPacket.cs` | Compare line-by-line during port; likely whitespace or constant ordering. |
| `SdapUdp.cs` | Same. |
| `SdcpTcp.cs` | Const field placement only in sampled diff. |
| `VmaContainer.cs` | Merge any behavioral delta if found. |
| `VmcCommand.cs` | Merge if any. |
| `VmsCommand.cs` | Merge if any. |
| `VmsContainer.cs` | Merge if any. |

### MonitorNetwork vs LMD_AutoWhiteBalance/DaiginjoSdcp

| File | Notes |
|------|--------|
| `VmsCommand.cs` | **Missing** in DaiginjoSdcp — not used by that tool. |
| Others | Differ; port from root `MonitorNetwork` and spot-check Daiginjo for LMD-specific quirks. |

## Application map (who uses what)

| Application | Path | SDCP usage |
|-------------|------|------------|
| LMD Auto White Balance | [LMD_AutoWhiteBalance/](LMD_AutoWhiteBalance/) | `DaiginjoSdcp`: VMC (`VmcCommand`), VMA adjustment helpers; probe hardware separate. |
| Monitor firmware updater | [Monitor_Update/VerUpTool/](Monitor_Update/VerUpTool/) | `MonitorNetwork`: VMC strings in `ControlVmcCommand.cs`, VMA upgrade (`VmaServiceCommand`), VMS where used. |
| BVM Auto White Adjustment | [Monitor_AutoWhiteAdjustment/](Monitor_AutoWhiteAdjustment/) | Uses `MonitorNetwork` patterns (verify call sites when mining VMC catalog). |

## MSSONY decompilation

Under [MSSONY/](MSSONY/): Hex-Rays C output (e.g. `controler_sdcp.vxe.c`). Use as **secondary** reference for timing/socket behavior; symbols are renamed. Do not treat as primary API contract.

## SDK mapping (target)

| Legacy | New (MonitorControlSDK) |
|--------|-------------------------|
| `SdcpPacket` | `Sony.MonitorControl.Protocol.SdcpMessageBuffer` |
| `SdapPacket` | `Sony.MonitorControl.Protocol.SdapMessage` / buffer helpers |
| `SdcpTcp` | `Sony.MonitorControl.Transport.SdcpConnection` |
| `SdapUdp` | `Sony.MonitorControl.Transport.SdapDiscovery` |
| `VmcCommand` / `VmcContainer` | `Sony.MonitorControl.Clients.VmcClient` + `VmcTokenizer` |
| `VmsCommand` / `VmsContainer` | `Sony.MonitorControl.Clients.VmsClient` + payload builders |
| `Vma*` | `Sony.MonitorControl.Clients.VmaClient` |

## VMC string catalog source list

Mine `sendCommand(` and VMC-related literals from:

- `Monitor_Update/VerUpTool/*.cs`
- `LMD_AutoWhiteBalance/SMAutoWB/*.cs`
- `Monitor_AutoWhiteAdjustment/BvmAutoWhiteBalanceTool/*.cs`

Output: [docs/spec/vmc-string-catalog.md](../spec/vmc-string-catalog.md).
