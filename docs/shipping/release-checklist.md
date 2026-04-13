# Release checklist

## Pre-release

- [ ] Version bump in [MonitorControlSDK.csproj](../../src/MonitorControlSDK/MonitorControlSDK.csproj) `<Version>`.
- [ ] If **HTTP routes or request bodies** changed: `bash scripts/fetch-openapi.sh` and commit [openapi/monitorcontrol.openapi.json](../../openapi/monitorcontrol.openapi.json).
- [ ] `dotnet pack src/MonitorControlSDK/MonitorControlSDK.csproj -c Release` produces `.nupkg` + symbols.
- [ ] [CHANGELOG.md](../../CHANGELOG.md) entry for user-visible changes (docs, API, protocol notes).
- [ ] Run full solution build + test on Windows target OS.

## Package metadata

- `PackageId`: `MonitorControl.Sdk`
- `PackageReadmeFile`: embedded README from SDK project
- License: MIT placeholder — **replace** if your organization requires a different license.

## Post-release

- Tag `vX.Y.Z` in git.
- Publish to private or public feed with `dotnet nuget push`.
