# Implementation Plan: Automated PDFium Bindings Update Pipeline

## Context

PDFiumCore wraps [bblanchon/pdfium-binaries](https://github.com/bblanchon/pdfium-binaries) into a .NET NuGet package by converting C headers to C# P/Invoke via CppSharp. The current process is manual: run the generator locally on Windows, commit, tag, push — then the existing `dotnet.yml` workflow builds/packs/publishes on tag push. This plan automates the entire cycle with a bi-weekly GitHub Action that detects new upstream releases, generates bindings on Ubuntu, and opens a PR.

### Decisions Made

| Decision | Choice |
|---|---|
| Runner OS | **Ubuntu** — fix generator for cross-platform CppSharp |
| Version detection | **GitHub API** releases endpoint |
| On new version | **Create PR** (not direct push) for review |
| Triggers | **Bi-weekly cron + workflow_dispatch** |

---

## 1. New Workflow: `.github/workflows/check-update.yml`

### Trigger Configuration

```yaml
on:
  schedule:
    - cron: '0 8 1,15 * *'   # 1st and 15th of each month, 08:00 UTC
  workflow_dispatch:
    inputs:
      force_update:
        description: 'Force update even if version matches'
        type: boolean
        default: false
```

### Job: `check-and-update`

**Runs-on:** `ubuntu-latest`

#### Step 1 — Detect Latest Upstream Release

Use `gh api` or `curl` against `https://api.github.com/repos/bblanchon/pdfium-binaries/releases/latest` to fetch:
- `name` field (e.g. `"PDFium v134.0.6996.0"`) — parse version from this
- `id` field — numeric release ID needed by the generator
- `tag_name` — e.g. `chromium/6996`

Store as step outputs: `UPSTREAM_VERSION`, `RELEASE_ID`, `TAG_NAME`.

#### Step 2 — Read Current Version

Parse `src/Directory.Build.props` for the `<Version>` element value. Store as `CURRENT_VERSION`.

#### Step 3 — Compare Versions

Short-circuit the workflow if `UPSTREAM_VERSION == CURRENT_VERSION` (unless `force_update` input is true). Use a simple string comparison — versions follow `major.minor.patch.revision` format and always increment.

#### Step 4 — Checkout with Submodules

```yaml
- uses: actions/checkout@v4
  with:
    submodules: true
    fetch-depth: 0
```

Required because the CppSharp submodule at `src/CppSharp/` is compiled and linked into the generator.

#### Step 5 — Setup .NET 8

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'
```

#### Step 6 — Install Linux Build Dependencies

CppSharp's libclang parser needs native libs on Ubuntu:

```bash
sudo apt-get update && sudo apt-get install -y libclang-dev
```

Exact packages TBD — may also need `libc6-dev` for standard C headers if CppSharp's bundled clang headers are insufficient. Validate during implementation.

#### Step 7 — Run Bindings Generator

```bash
dotnet build src/PDFiumCoreBindingsGenerator/PDFiumCoreBindingsGenerator.csproj -c Release
dotnet src/PDFiumCoreBindingsGenerator/bin/Release/net8.0/PDFiumCoreBindingsGenerator.dll $RELEASE_ID true 0
```

This invokes `Program.Main` with:
- `args[0]` = release ID (e.g. `198028030`)
- `args[1]` = `true` (generate bindings)
- `args[2]` = `0` (minor revision)

Generator will: download pdfium-win-x64 tarball, extract headers, run CppSharp, write `PDFiumCore.cs`, update `Directory.Build.props`, update `download_package.sh`.

#### Step 8 — Build & Test

```bash
dotnet build src/PDFiumCore -c Release
dotnet test src/PDFiumCore.Tests -c Release
```

Validate that the newly generated bindings compile and pass tests before opening a PR.

#### Step 9 — Create PR

Use `peter-evans/create-pull-request@v6` (or `gh pr create`):

```yaml
- uses: peter-evans/create-pull-request@v6
  with:
    token: ${{ secrets.GITHUB_TOKEN }}
    branch: update/pdfium-${{ steps.detect.outputs.UPSTREAM_VERSION }}
    title: "Update PDFium to v${{ steps.detect.outputs.UPSTREAM_VERSION }}"
    body: |
      Automated update from bblanchon/pdfium-binaries.
      - Upstream release: ${{ steps.detect.outputs.TAG_NAME }}
      - Version: ${{ steps.detect.outputs.UPSTREAM_VERSION }}
    commit-message: "PDFium version v${{ steps.detect.outputs.UPSTREAM_VERSION }}"
    labels: automated-update
```

Key config: `commit-message` should match the project's existing convention (`PDFium version v{version} chromium/{id} [master]`).

If using `gh pr create` instead, configure git identity first:
```bash
git config user.name "github-actions[bot]"
git config user.email "github-actions[bot]@users.noreply.github.com"
```

---

## 2. Cross-Platform CppSharp Fix: `src/PDFiumCoreBindingsGenerator/PDFiumCoreLibrary.cs`

### Problem

Line 37: `driver.ParserOptions.SetupMSVC(VisualStudioVersion.Latest)` — fails on Linux (no Visual Studio).

### Solution

Replace the `SetupMSVC` call with platform-conditional setup:

```csharp
// Signature of the modified Setup method:
public void Setup(Driver driver)
```

**Algorithm:**
1. Check `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`
2. **Windows path** (preserves existing behavior): call `SetupMSVC(VisualStudioVersion.Latest)` as before
3. **Linux path**: skip `SetupMSVC` entirely. CppSharp bundles its own clang system headers at `lib/clang/14.0.0/include` which are already added to `module.IncludeDirs`. For standard C headers (`stddef.h`, `stdint.h`, etc.), these bundled headers should suffice since PDFium headers are pure C. If not, add `/usr/include` and architecture-specific include path (e.g. `/usr/include/x86_64-linux-gnu`).

**Key insight:** The generator already `Undefines("_WIN32")` to produce platform-neutral bindings, so the generated C# output should be identical regardless of host OS — the P/Invoke signatures don't change.

### Validation Concern

After implementation, diff the Linux-generated `PDFiumCore.cs` against the current Windows-generated version. Any differences beyond the `// Built on:` timestamp line indicate a problem. The bindings MUST be byte-identical (excluding metadata comments).

---

## 3. Existing Workflow Changes: `.github/workflows/dotnet.yml`

### No structural changes required

The existing `dotnet.yml` remains the **publish pipeline**. It triggers on tag push (`v*`) and handles:
- Build, pack, test
- GitHub release creation
- NuGet push

### Tag + Merge Flow

After the PR from the check-update workflow is reviewed and merged:
1. Maintainer creates a tag `v{version}` on the merge commit
2. Existing `dotnet.yml` triggers and publishes

**Optional enhancement:** Add a separate job or step in `dotnet.yml` that auto-tags on merge of PRs with the `automated-update` label. This would use:
```bash
VERSION=$(grep '<Version>' src/Directory.Build.props | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/')
git tag "v${VERSION}"
git push origin "v${VERSION}"
```

This is optional — the maintainer can continue tagging manually if preferred.

---

## 4. File Inventory

| File | Action | Description |
|---|---|---|
| `.github/workflows/check-update.yml` | **CREATE** | New bi-weekly update check workflow |
| `src/PDFiumCoreBindingsGenerator/PDFiumCoreLibrary.cs` | **MODIFY** | Add platform detection, conditional `SetupMSVC` vs Linux path |
| `.github/workflows/dotnet.yml` | **OPTIONAL MODIFY** | Add auto-tag on merge of update PRs |

No changes to `Program.cs`, `PDFiumCore.csproj`, `Directory.Build.props`, or `download_package.sh` — these are handled at runtime by the existing generator.

---

## 5. Secrets & Permissions

### Required repository settings

- **`GITHUB_TOKEN`** — already available, used by `peter-evans/create-pull-request` to create branches and PRs. Needs `contents: write` and `pull-requests: write` permissions.
- **`ORG_NUGET_AUTH_TOKEN`** — already configured, only used by `dotnet.yml` on tag push (no change needed).

### Workflow permissions block

```yaml
permissions:
  contents: write
  pull-requests: write
```

---

## 6. Edge Cases & Failure Modes

| Scenario | Handling |
|---|---|
| **No new release** | Step 3 comparison exits early with success. No PR created. |
| **PR already exists** for this version | `peter-evans/create-pull-request` is idempotent — updates existing PR if branch name matches. |
| **bblanchon NuGet packages lag behind GitHub release** | Build will fail at `dotnet build` (can't resolve `bblanchon.PDFium.* Version=$(Version)`). PR is still created but CI fails, signaling maintainer to wait. Consider adding a NuGet version existence check before proceeding. |
| **CppSharp generation fails on Linux** | Job fails, no PR created. Investigate clang/include path issues. |
| **Tests fail with new bindings** | Build+test step fails before PR creation. No broken PR is opened. |
| **GitHub API rate limit** | Use `GITHUB_TOKEN` auth header for 5000 req/hr instead of anonymous 60 req/hr. |
| **Upstream release name format changes** | Version parsing in Step 1 would fail. Add a validation step that checks the extracted version matches `\d+\.\d+\.\d+\.\d+`. |

---

## 7. Verification Plan

### Local validation (before merging this automation branch)

1. **Cross-platform generator test:** Run `PDFiumCoreBindingsGenerator` on WSL/Ubuntu, compare output `PDFiumCore.cs` against current Windows-generated file. Diff should only show timestamp line.
2. **Workflow syntax validation:** `actionlint` or GitHub's workflow editor to validate YAML.

### Post-merge validation

3. **Manual dispatch test:** Trigger `check-update.yml` via `workflow_dispatch` with `force_update: true`. Verify:
   - Upstream version detection works
   - Generator runs on Ubuntu runner
   - PR is created with correct branch name, title, changed files
   - PR passes existing CI checks (`dotnet.yml` on PR trigger)
4. **End-to-end:** Merge the generated PR, create tag, verify NuGet package publishes successfully.

### Ongoing monitoring

5. Bi-weekly runs should appear in Actions tab. If no upstream updates, runs complete quickly with "no update needed" in logs.
