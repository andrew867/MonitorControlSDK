# VMA firmware and service operations

VMA uses SDCP **V3** framing with item **0xF000** (`setupVma`).

## Service commands (subset)

Implemented on `LegacyVmaContainer` and wrapped by **`VmaClient`** for safe read-only examples:

- Control software version, kernel version, RTC read.
- Adjustment mode (`jigAdjMode`).

## Dangerous operations

The following exist in `LegacyVmaContainer` and must **not** be exposed in sample CLIs without explicit operator acknowledgement:

- `serviceUpgradeChunk`, `serviceUpgradeKernel`, `serviceUpgradeFPGA`, `serviceUpgradeRestart` — can brick hardware if misused.
- Factory/service adjustment writes (`jig*` beyond read-only demos).

## SDK policy

- **`VmaClient`** in this repo only wraps non-destructive reads and a single adjustment-mode example for parity testing.
- For firmware update flows, copy patterns from `Monitor_Update/VerUpTool` only inside controlled maintenance tooling, with hardware-specific validation.

## Reference

[VmaContainer.cs](../../MonitorNetwork/MonitorNetwork/VmaContainer.cs), [VmaServiceCommand.cs](../../MonitorNetwork/MonitorNetwork/VmaServiceCommand.cs).
