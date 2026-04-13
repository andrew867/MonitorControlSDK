# Release checklist

## Pre-release

- [ ] Version bump in [MonitorControlSDK.csproj](../../src/MonitorControlSDK/MonitorControlSDK.csproj) `<Version>`.
- [ ] `dotnet pack src/MonitorControlSDK/MonitorControlSDK.csproj -c Release` produces `.nupkg` + symbols.
- [ ] `CHANGELOG` entry (optional file) summarizing breaking API changes.
- [ ] Run full solution build + test on Windows target OS.

## Package metadata

- `PackageId`: `Sony.MonitorControl`
- `PackageReadmeFile`: embedded README from SDK project
- License: MIT placeholder — **replace** if your organization requires a different license.

## Post-release

- Tag `vX.Y.Z` in git.
- Publish to private or public feed with `dotnet nuget push`.
