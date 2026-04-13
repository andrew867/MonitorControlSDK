# VMA (SDCP item 0xF000) wire reference

VMA payloads are **binary**, not ASCII. The layout is built exclusively in [`LegacyVmaContainer`](../../src/MonitorControlSDK/Internal/LegacyVmaContainer.cs). High-level sends are in [`VmaClient`](../../src/MonitorControlSDK/Clients/VmaClient.cs).

## Payload structure (common pattern)

Most builders set:

- `data[0]` — **major class**: `0` = adjustment (jig), `1` = service, `2` = direct backlight query.
- `data[1]` — **sub-command** within that class.
- Further bytes — arguments (big-endian 16-bit splits where noted in each method).

`length` is the inclusive payload byte count for the VMA container.

## Adjustment class (`data[0] == 0`)

Sub-commands (`data[1]`) are named `CMD_ADJUSTMENT_*` in source. Public builders include:

| `data[1]` | Builder | Purpose (from method body) |
|-----------|-----------|----------------------------|
| 0 | `jigAdjMode` | Factory adjustment mode byte `data[2]` |
| 14 | `jigWhiteLevel` | 16-bit level |
| 18 | `jigColorTemp_xyY` | x, y, Y each 16-bit BE |
| 29 | `jigServiceAdjMode` | Service adjustment mode byte |
| 30 | `jigUfAdjMode` | Universal-function adjust mode |
| 31–38 | `jigUf*` | UF target, measure, position, probe sense, matrix calc, probe offset |

### “Bitmask” vs mode byte

The nested types `ParamAdjMode`, `ParamServiceAdjMode`, `ParamUfAdjMode`, `ParamUfProbeSense` define **single-byte mode enumerations** (constants `0`, `1`, `2`, …) — they are **not** bit masks in the C# source. When a method takes `byte md`, treat it as an **opaque mode index** unless your service manual maps the meaning per product line.

## Service class (`data[0] == 1`)

| `data[1]` | Constant name | Builder / `VmaClient` |
|-----------|----------------|------------------------|
| 1 | `CMD_SERVICE_SET_OPERATION_TIME` | (container only) |
| 2 | `CMD_SERVICE_GET_OPERATION_TIME` | (container only) |
| 3 | `CMD_SERVICE_BACKLIGHT_RESET` | (container only) |
| 4 | `CMD_SERVICE_RESTORE_FACTORY` | (container only) |
| 5 | `CMD_SERVICE_EDID_WP` | (container only) |
| 6 | `CMD_SERVICE_SET_RTC` | (container only) |
| 7 | `CMD_SERVICE_GET_RTC` | `serviceGetRTC` → `SendGetRtc` |
| 8 | `CMD_SERVICE_UPGRADE_CHUNK` | `serviceUpgradeChunk` → `SendFirmwareUpgradeChunk` |
| 9 | `CMD_SERVICE_UPGRADE_KERNEL` | `serviceUpgradeKernel` → `SendFirmwareUpgradeKernel` |
| 10 | `CMD_SERVICE_UPGRADE_FPGA` | `serviceUpgradeFPGA` → `SendFirmwareUpgradeFpga` |
| 11 | `CMD_SERVICE_UPGRADE_RESTART` | `serviceUpgradeRestart` → `SendFirmwareUpgradeRestart` |
| 12 | `CMD_SERVICE_GET_SOFTWARE_VERSION` | `GetControlSoftwareVersion` |
| 13 | `CMD_SERVICE_GET_KERNEL_VERSION` | `GetKernelVersion` |
| 14 | `CMD_SERVICE_GET_FPGA_VERSION` | `GetFPGA1Version` / `GetFPGA2Version` / `GetFPGACoreVersion` (discriminated by `data[2]`) |

## Direct backlight class (`data[0] == 2`)

Sub-commands `0`–`12`: serial, version, NVM version, PWM, color sensor, BL power, room/panel/BLM temperature, fan status, ECS read, register read, LED open check — see method names `directBL*` in `LegacyVmaContainer`.

## Safety

Wrong ordering of **8–11** during firmware can brick hardware. This repository exposes **wire-accurate** APIs; it does **not** implement a complete validated OTA state machine. Read [guide/firmware-updates.md](../guide/firmware-updates.md) before use.
