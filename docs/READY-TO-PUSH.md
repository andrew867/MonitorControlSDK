# Ready to push to GitHub

Upstream (HTTPS only): **https://github.com/andrew867/MonitorControlSDK.git**

Nothing in this document runs automatically. **Do not push** until you have reviewed the working tree and CI artifacts.

## Pre-flight

1. `dotnet build MonitorControl.sln -c Release`
2. `dotnet test MonitorControl.sln -c Release`
3. `git status` — ensure no accidental firmware blobs or local-only trees are staged.

## Set remote and push (when you approve)

```bash
git remote remove origin 2>/dev/null || true
git remote add origin https://github.com/andrew867/MonitorControlSDK.git
git branch -M main
git push -u origin main
```

Use **HTTPS** URLs only (not `git@github.com:…`).

## After push

Confirm GitHub Actions **build-and-pack** passes on Windows, Linux, and macOS; download artifacts to verify `monitorctl-*` and `.nupkg` outputs.
