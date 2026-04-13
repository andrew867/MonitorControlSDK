# Testing strategy

## Unit tests (default CI)

Located in [tests/MonitorControlSDK.Tests](../../tests/MonitorControlSDK.Tests).

- **Framing**: V3/V4 header constants and item numbers (`SdcpMessageBufferTests`).
- **Codec math**: VMS float encode/decode round-trip (`VmsFloatCodecTests`).

These use **real buffers** (no socket mocks) to match on-wire layout.

## Golden / snapshot tests (recommended next step)

Capture **hex dumps** from a lab monitor for representative `VmsCommandEngine` sequences and assert byte-for-byte equality after builders run. Store fixtures under `tests/Fixtures/` (not yet added).

## Hardware integration

Mark tests with `[Trait("Category", "Hardware")]` and exclude from default CI:

```bash
dotnet test --filter "Category!=Hardware"
```

Requires stable IP, model, and firmware matrix documentation.

## Local NuGet

Repository [nuget.config](../nuget.config) maps all packages to **nuget.org** so `dotnet test` resolves under `PackageSourceMapping` user policies.
