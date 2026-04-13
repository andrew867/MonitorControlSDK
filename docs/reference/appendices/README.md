# Generated appendices

These text files are **regenerated from source** so the documentation stays exhaustive without hand-maintaining 300+ lines.

```bash
rg "private const byte CMD_" src/MonitorControlSDK/Internal/LegacyVmsContainer.cs > docs/reference/appendices/vms-opcode-constants.txt
rg "^\tpublic int send" src/MonitorControlSDK/Protocol/VmsCommandEngine.cs > docs/reference/appendices/vms-engine-send-methods.txt
```

- `vms-opcode-constants.txt` — full `CMD_*` tree.
- `vms-engine-send-methods.txt` — every `VmsCommandEngine.send*` entry point.
