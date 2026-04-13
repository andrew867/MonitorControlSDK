# Ready to push

This repository is normally configured with **two remotes**:

| Remote | Typical URL | Role |
|--------|----------------|------|
| **`origin`** | Self-hosted Git (example: `git@git.example.com:…`) | Day-to-day pushes, internal CI |
| **`github`** | `https://github.com/andrew867/MonitorControlSDK.git` | Public mirror, GitHub Actions, releases |

Nothing in this document runs automatically. **Do not push** until you have reviewed the working tree and local build results.

## Pre-flight

1. `dotnet build MonitorControl.sln -c Release`
2. `dotnet test MonitorControl.sln -c Release`
3. If HTTP routes changed: `bash scripts/fetch-openapi.sh` and stage `openapi/monitorcontrol.openapi.json`.
4. `git status` — ensure no accidental firmware blobs or local-only trees are staged.

## Push both servers (after you approve)

Replace branch name if you are not on `master`:

```bash
git push origin master
git push github master
```

For a **tagged release** (after bumping `<Version>` in `src/MonitorControlSDK/MonitorControlSDK.csproj` and updating [CHANGELOG.md](../CHANGELOG.md)):

```bash
git tag -a v0.3.0 -m "Release v0.3.0"
git push origin v0.3.0
git push github v0.3.0
```

## HTTPS-only GitHub remote (alternative)

If you prefer HTTPS for `github`:

```bash
git remote set-url github https://github.com/andrew867/MonitorControlSDK.git
```

## After push

Confirm **GitHub Actions** `build-and-pack` passes on Windows, Linux, and macOS; download artifacts to verify `monitorctl-*` and `.nupkg` outputs. If you use **GitLab CI**, confirm the pipeline on your self-hosted runners.
