# Generated appendices

These text files are **regenerated from source** (and from the `references/` C# tree for VMC literals) so documentation stays exhaustive without hand-maintaining hundreds of lines.

## One command (preferred)

From the repository root:

```bash
bash scripts/regen-appendices.sh
```

This refreshes:

| Output | Source |
|--------|--------|
| `vms-opcode-constants.txt` | `rg "private const byte CMD_" src/MonitorControlSDK/Internal/LegacyVmsContainer.cs` |
| `vms-engine-send-methods.txt` | `rg "^\tpublic int send" src/MonitorControlSDK/Protocol/VmsCommandEngine.cs` |
| `vmc-stat-tokens-from-references.txt` | `references/**/*.cs` — all literal `STATget` / `STATset` / `sendCommandBroadCast` strings (see script for exact `rg` patterns) |
| `pvm-740-vmc-catalog-from-manual.txt` | **Static** — PVM-740 programmer manual excerpt (ManualsLib pages 9–12); update if you re-key from a newer PDF |

## Manual equivalents (if you cannot run bash)

```bash
rg "private const byte CMD_" src/MonitorControlSDK/Internal/LegacyVmsContainer.cs > docs/reference/appendices/vms-opcode-constants.txt
rg "^\tpublic int send" src/MonitorControlSDK/Protocol/VmsCommandEngine.cs > docs/reference/appendices/vms-engine-send-methods.txt
bash scripts/regen-appendices.sh   # still needed for vmc-stat-tokens-from-references.txt
```

## Human guides

- VMS architecture: [vms-overview.md](../vms-overview.md)
- VMC categories + links: [vmc-command-surface.md](../vmc-command-surface.md)
- References parity checklist: [references-parity.md](../references-parity.md)
