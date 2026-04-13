# CI/CD — GitHub (public) + GitLab (self-hosted)

This repository is wired for **two remotes**: GitHub for public builds, releases, and community visibility; GitLab for **self-hosted** Linux/Windows runners (internal iteration, private mirrors, or pre-publish validation).

| Platform | Config | Typical use |
|----------|--------|-------------|
| **GitHub Actions** | [`.github/workflows/build.yml`](../.github/workflows/build.yml) | PR + `main`/`master` + `v*` tags; matrix **Ubuntu / Windows / macOS**; artifacts; **GitHub Release** on tags; **GitHub Packages** (NuGet); optional **NuGet.org** |
| **GitLab CI/CD** | [`.gitlab-ci.yml`](../.gitlab-ci.yml) | MR + branches + `v*.*.*` tags; **parallel Linux + Windows** builds; **GitLab Release**; **GitLab Package Registry**; optional **NuGet.org** |
| **Dependabot** | [`.github/dependabot.yml`](../.github/dependabot.yml) | Weekly NuGet + GitHub Actions bumps (GitHub only) |

---

## GitHub Actions

### Triggers

- Push to `main` or `master`
- Push tags matching `v*` (e.g. `v0.1.1`)
- Pull requests
- Manual **Run workflow** (`workflow_dispatch`)

### Jobs

1. **`build`** (matrix) — `dotnet restore`, `build`, `test`, `pack` the SDK, `publish` **monitorctl** for `linux-x64`, `win-x64`, `osx-arm64`. Uploads one artifact per OS.
2. **`release`** (tags only, after all matrix jobs succeed) — downloads matrix artifacts, collects one `.nupkg` + `.snupkg` (if present), zips each `monitorctl-*` folder, creates a **GitHub Release** with `softprops/action-gh-release`, pushes the `.nupkg` to **GitHub Packages**, then optionally to **NuGet.org**.

### Secrets & permissions

| Item | Purpose |
|------|---------|
| `GITHUB_TOKEN` | Default; `release` job uses `contents: write` + `packages: write` for releases and GitHub Packages |
| `NUGET_API_KEY` | Optional repository **secret** — [NuGet API key](https://www.nuget.org/account/apikeys) with **Push** scope for `MonitorControl.Sdk`. If unset, the NuGet.org step logs a skip and exits successfully |

### Badges

The root [README](../README.md) build badge points at `build.yml` on `master`.

### When GitHub Packages push fails

- Confirm **Packages** are enabled for the org/user.
- The feed URL is `https://nuget.pkg.github.com/OWNER/index.json` where `OWNER` is `github.repository_owner`.
- If you do not want GitHub Packages, remove or comment out the **Push to GitHub Packages** step in `build.yml` (NuGet.org-only flow).

---

## GitLab CI (self-hosted runners)

### Runner tags

Register runners and assign tags (Settings → CI/CD → Runners → edit tags):

| Tag | Role |
|-----|------|
| `gitlab-linux` | Linux jobs (build, pack, most release jobs) |
| `gitlab-windows` | Windows build + `win-x64` **monitorctl** |

Install **.NET 8 SDK** on each runner. For **Docker** executors instead, uncomment the `image: mcr.microsoft.com/dotnet/sdk:8.0` block documented at the top of `.gitlab-ci.yml` and adjust jobs to use it (you can drop host SDK installs).

### Stages

1. **`build:linux` / `build:windows`** — restore, build, test, publish **monitorctl** for `linux-x64` / `win-x64`; artifacts per job.
2. **`package:nupkg`** — packs `MonitorControl.Sdk` into `publish/nupkg/` (runs on Linux after `build:linux` without needing its artifacts).
3. **`release:gitlab`** — GitLab **Release** record for semver tags (`vMAJOR.MINOR.PATCH`).
4. **`publish:gitlab-nuget`** — `dotnet nuget push` to this project’s **GitLab NuGet Package Registry** (uses `CI_JOB_TOKEN`). Requires GitLab **15.1+** and project setting allowing the job token to access the registry (see GitLab docs *“Job token permissions”*).
5. **`release:nuget-org`** — pushes to **NuGet.org** when the masked variable **`NUGET_API_KEY`** is set; otherwise prints a skip message.

### Custom runner tags

If you cannot use `gitlab-linux` / `gitlab-windows`, search-replace those strings in `.gitlab-ci.yml` to match your tags.

---

## Semver tags

Use **annotated** tags matching `vMAJOR.MINOR.PATCH` (e.g. `v0.1.2`) so both pipelines run **release** stages consistently.

```bash
git tag -a v0.1.2 -m "Release v0.1.2"
git push origin v0.1.2
git push github v0.1.2   # if you use a second remote
```

Bump `<Version>` in [`src/MonitorControlSDK/MonitorControlSDK.csproj`](../src/MonitorControlSDK/MonitorControlSDK.csproj) before tagging.

---

## Suggested workflow

1. Day-to-day: push branches / MRs to **GitLab** for fast feedback on your runners.
2. When stable: merge to `master`, push to **GitHub**, tag `v*`, let Actions attach release assets and (if configured) publish to NuGet.org + GitHub Packages.
3. Keep GitLab’s **NuGet.org** job disabled by omitting `NUGET_API_KEY` there if GitHub is the single source of public publishes — or set the key on both for redundancy.

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| GitLab: *no runners* | Tags on jobs match runner tags; runner not paused |
| GitLab: *JOB_TOKEN push 403* | Project → Settings → CI/CD → Job token allowlist / package registry permissions |
| GitHub: *release job missing zips* | Matrix artifacts must include `monitorctl-*` dirs; `find` in workflow lists three RIDs |
| GitHub: *NuGet.org 401* | Rotate `NUGET_API_KEY`; ensure key includes the correct package ID scope |
| Dependabot PR noise | Tweak [`.github/dependabot.yml`](../.github/dependabot.yml) `ignore` / `groups` / `open-pull-requests-limit` |
