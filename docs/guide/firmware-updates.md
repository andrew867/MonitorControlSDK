# Firmware updates (VMA service class)

This document describes **what this SDK can place on the wire** and the **minimum conceptual sequence** for monitor control software. It is **not** a substitute for Sony service documentation or bench validation.

## Source of truth in code

| Step | VMA wire (`data[0]=1`) | `VmaClient` method |
|------|-------------------------|---------------------|
| Declare kernel image size | sub `9`, BE `int32` size | `SendFirmwareUpgradeKernel` |
| Declare FPGA image size | sub `10`, BE `int32` size | `SendFirmwareUpgradeFpga` |
| Stream chunk index | sub `8`, `data[2]` = chunk index | `SendFirmwareUpgradeChunk` |
| Reboot loader | sub `11`, no args | `SendFirmwareUpgradeRestart` |

Builders: [`LegacyVmaContainer`](../../src/MonitorControlSDK/Internal/LegacyVmaContainer.cs) (`serviceUpgrade*` methods).  
Tests proving byte layout: [`FirmwareWireEncodingTests`](../../tests/MonitorControlSDK.Tests/FirmwareWireEncodingTests.cs).

## Typical high-level sequence (inferred)

1. Establish normal SDCP session (TCP **53484**).
2. Put device in **service / upgrade** readiness (often involves on-device UI or prior VMA commands — **not implemented** as a wizard in this repo).
3. Send **kernel size**, stream payload with **chunk indices** as required by device protocol (index semantics are firmware-specific).
4. Repeat for **FPGA** if applicable.
5. Issue **restart** and wait for device to come back on the network.

**Gap:** This repository does **not** ship a complete binary pack parser, CRC/chunk size negotiation, or progress UI. You must supply those from your validated process.

## Version reads (safe-ish)

Before/after upgrade, use:

- `SendGetControlSoftwareVersion`
- `SendGetKernelVersion`
- `SendGetFpga1Version` / `SendGetFpga2Version` / `SendGetFpgaCoreVersion`

## Legal and safety

Incorrect images or wrong ordering can **brick** hardware. Use isolated VLANs, redundant power, and recovery paths mandated by your facility.

## External context

Sony **projector** protocol manuals discuss **SDAP** on UDP **53862** (same port as this monitor stack) but use **ADCP** on TCP **53595** for control — **different** from monitor SDCP **53484**. See [reference/external-sources.md](../reference/external-sources.md).
