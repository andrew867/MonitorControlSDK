# VMA firmware and service (short)

Wire layout and opcode tables: [**reference/vma-wire-reference.md**](../reference/vma-wire-reference.md).

Firmware sequence and API: [**guide/firmware-updates.md**](../guide/firmware-updates.md).

Safe read-only examples use [`VmaClient`](../../src/MonitorControlSDK/Clients/VmaClient.cs) (`SendGetControlSoftwareVersion`, `SendGetKernelVersion`, `SendGetRtc`). Dangerous upgrade entry points are exposed with XML warnings on the same class.
