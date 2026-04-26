# Development Guide

[English](DEVELOPMENT.md) | [简体中文](DEVELOPMENT_zh_CN.md)

Release maintenance in this project is easiest to understand as three paths:

- **Routine development**: normal development and commits, without automatically building full release packages
- **Test packaging / repair**: use `manual-release-test` when a package needs to be validated or an existing Release needs missing files rebuilt
- **Production release**: once the package is confirmed to be good, let `release-please` handle the official release

The goal is straightforward:

- routine development should not waste build resources
- validation should happen separately, without polluting the public release page
- production release should only handle versions that are already confirmed to be usable

---

## Routine development

During routine development, there is no reason to build full ZIP, EXE, and MSIX packages on every push.

The reason is practical:

- full packaging is expensive
- active development would generate too many low-value builds
- most commits are not worth turning into complete installable outputs

So routine development stays simple: normal commits, no automatic full-package build.

When a change really needs packaging validation, trigger the test packaging flow on demand.

---

## Rule for following proven references (important)

When you already have a **real, known-working** reference repository, workflow, or script, the default strategy is not “clean it up first.” It should be:

> **Follow first, understand second; respect the interface before deciding whether to refactor it.**

This matters especially for GitHub Actions, packaging scripts, CLI calls, manifest settings, and other integration-heavy configuration.

### Why this rule exists

In this kind of setup, a working reference is usually not just a sample. It is often:

- already validated against real platform behavior
- already shaped by previous failures and compatibility issues
- carrying interface constraints in places that may look stylistically inconsistent

If those constraints are not understood first, “small cleanup” changes can easily break a flow that was already known to work.

### Three different naming layers must be separated

In workflows, at least these three naming layers should be treated separately:

1. **secret storage name**
   - for example: `secrets.GH_TOKEN`

2. **action input name**
   - for example: `with: token: ...`

3. **environment variable name expected by the downstream tool**
   - for example: `env: GITHUB_TOKEN: ...`

These names do not mean the same thing. A rename on the value source side does not automatically justify renaming the consumer-side interface name.

### Typical example: why only the right side should change

If a proven reference uses:

```yaml
with:
  token: ${{ secrets.GH_TOKEN }}

env:
  GITHUB_TOKEN: ${{ secrets.GH_TOKEN }}
```

then the meaning is:

- `secrets.GH_TOKEN` = where the value comes from
- `with.token` = the input interface expected by the action
- `env.GITHUB_TOKEN` = the interface name expected by the downstream CLI/tool

So the correct approach is:

- changing the value source may be fine
- changing the consumer-side interface name just for apparent consistency is usually a mistake

Supporting evidence:

- The official GitHub CLI environment documentation explicitly states that `gh` reads established names such as `GH_TOKEN` and `GITHUB_TOKEN`; it does not care about the repository secret name by itself  
  Documentation: <https://cli.github.com/manual/gh_help_environment>

That reinforces the separation:

- `${{ secrets.GH_TOKEN }}` on the right is the **value source**
- `GITHUB_TOKEN` / `GH_TOKEN` on the left is the **consumer-facing interface name**
- this kind of mapping should be decided from the downstream tool contract first, not from visual naming consistency

### Default working method

When a reference is already known to run successfully, use this order:

1. assume the reference shape has a reason
2. identify whether the left side is a consumer interface or a locally chosen name
3. avoid renaming interface names unless necessary
4. if a rename is considered, verify it with official documentation or real runtime evidence first
5. without strong evidence, prefer to preserve the proven reference shape

### One-sentence principle

> **A proven working reference is not something to casually normalize; it is something to first decode and preserve.**

---

## Local release prerequisites

If you want to validate ZIP / EXE locally before pushing to CI, make sure the machine has:

- .NET 8 SDK
- Visual Studio 2022/2026 or Build Tools with **MSBuild** and Windows 10/11 SDK (**SignTool**)
- Inno Setup 6 (`ISCC.exe`)

The release scripts now auto-detect these tools. If one is missing, the script should stop early with a direct message instead of failing later with an ambiguous packaging error.

### GitHub push / proxy prerequisite

If your network cannot reach GitHub directly, finish the proxy setup before trying to:

- push code to GitHub
- push `.github/workflows/*`
- validate GitHub Actions

See:

- [`PROXY.md`](PROXY.md)

Important:

- if `origin` still uses `git@github.com:...`, setting only `HTTP_PROXY` / `HTTPS_PROXY` may still leave `git push` broken
- if the goal is simply to get this project onto GitHub quickly, the default recommendation is: **switch the GitHub remote to HTTPS, then configure a Git proxy**

---

## Recommended local validation sequence

Use this order when validating a release candidate on your own machine:

1. `dotnet restore autojs6-dev-tools.slnx`
2. `dotnet build autojs6-dev-tools.slnx -c Release`
3. `dotnet test autojs6-dev-tools.slnx -c Release`
4. Build `win-x64` and `win-arm64` portable ZIP packages
5. Run `scripts/release/Test-PortablePackageSmoke.ps1` against the `win-x64` portable EXE
6. Build `win-x64` and `win-arm64` EXE installers
7. Confirm the files in `release-assets/` match the expected version, publisher, and SHA256 list

Use CI after local validation, not instead of it.

---

## Non-interactive CI safety and hang review

The release path has now been reviewed specifically for **“can this block waiting for a human?”** behavior.

### Confirmed high-risk interactive point

- `scripts/release/New-CodeSigningCertificate.ps1`
  - Risk source: importing a generated certificate into `TrustedPeople` / `Root`
  - Why it can hang: `Root` trust import may trigger Windows trust confirmation and stall in headless CI
  - Current behavior:
    - CI / non-interactive sessions skip trust-store import automatically
    - local trust import is **opt-in only**
    - enable only when you explicitly pass:
      - `-ImportToTrustedPeople`
      - `-ImportToRoot`

### Current release policy

- MSIX packaging is **temporarily disabled** in both `manual-release-test` and `release-please`
- current supported release outputs are:
  - ZIP
  - EXE installer
- MSIX remains a deferred follow-up item until certificate trust, signing verification, install flow, and end-user experience are validated end to end

### Reviewed release scripts that should not require manual confirmation in CI

- `scripts/release/Build-PortablePackage.ps1`
  - Uses `dotnet publish`
  - No interactive prompt path in the script itself

- `scripts/release/Build-InnoInstaller.ps1`
  - Uses `ISCC.exe`
  - No script-level confirmation prompt
  - If Inno Setup is missing, it should fail fast with a direct error

- `scripts/release/Build-MsixPackage.ps1`
  - currently kept in the repository for future work
  - not part of the active CI release path

- `scripts/release/Set-AppReleaseMetadata.ps1`
  - File-edit only, no interactive path

- `scripts/release/Test-PortablePackageSmoke.ps1`
  - Starts the app, waits a bounded number of seconds, then force-stops it
  - This is time-bounded, not an infinite confirmation wait

### Scripts that are local-install oriented and should not be used in CI

- `App/AppPackages/**/Add-AppDevPackage.ps1`
- `App/AppPackages/**/Install.ps1`

These generated install scripts are for local sideload / local package install flows and may involve certificate trust or install-time interaction. They are **not** part of the CI release packaging path and should not be called from GitHub Actions.

### Fast triage when a workflow looks “stuck”

If a Windows release job appears frozen:

1. check the **last printed log line**
2. identify the exact script / step name
3. assume the last visible operation is the first suspect
4. cancel the run if it is clearly waiting on a trust / install / UI action
5. fix the script to become fail-fast or non-interactive before rerunning

### Practical rule

> In CI, package generation is allowed; machine trust changes are not a safe default.

---

## Test packaging / repair

Test packaging and repair both use:

- GitHub Actions workflow: `manual-release-test`

This flow covers the two most common needs.

### 1. Checking whether the package is still usable

Typical examples:

- the workflow was changed
- packaging scripts were changed
- a release is coming and ZIP / EXE should be validated first

In that case, simply run `manual-release-test`.

The most common approach is:

- build from `main`
- use a normal test version
- do not upload to GitHub Release

That keeps all outputs in the current Actions run, where they can be downloaded and checked without affecting the public Release page.

### 2. Repairing an existing Release that is missing files

Typical examples:

- the official Release page already exists
- some ZIP / EXE files are missing
- upload failed partway through

In that case, a new version is usually not needed right away. The existing Release should be repaired first.

The correct approach is:

- rebuild from the already published tag
- keep the same version number
- allow upload to GitHub Release
- upload the rebuilt assets back to that same Release

That keeps release history clean and avoids unnecessary version churn.

### What actually matters when running test packaging

Not every input needs equal attention. In practice, only three things matter first:

#### Which code is being packaged

For a routine validation run, `main` is usually enough.

For a repair run, use the already published production tag.

#### Which version the package should show

Use a normal semantic version, such as:

- `0.0.1`
- `0.1.0`

#### Whether this run is only for validation or should upload to a Release

- if this is only a test, keep release upload off
- if this is repairing an existing Release, enable upload

Those are the decisions that matter most during test packaging.

---

## Production release

Official publishing is handled by:

- GitHub Actions workflow: `release-please`

The recommended order is:

1. merge regular feature and fix work into `main`
2. wait for `release-please` to create or update the release PR
3. do not merge it immediately
4. run `manual-release-test` first
5. confirm that the main package outputs are usable
6. merge the release PR
7. let `release-please` complete the official release

The benefits are practical:

- official version numbers remain stable
- production tags are not polluted by test actions
- if something goes wrong later, it is easier to trace the issue to a specific version

A good way to think about the two flows is:

- `manual-release-test` confirms “Is this package ready for users?”
- `release-please` confirms “Publish this as the official version.”

---

## Which outputs matter most to end users

Each full packaging run produces a full set of files, but the ones that matter most to ordinary users are still these two:

- **EXE**: the most familiar Windows installation experience
- **ZIP**: best for users who want to extract and run directly

If release quality is judged from the real end-user experience, the priority should be:

- EXE installs cleanly
- ZIP launches cleanly after extraction

Those two outputs define whether the release feels usable in practice.

---

## What to do when something goes wrong

### The test packaging flow fails

That usually means the issue is in the current code or packaging configuration, not in the idea of releasing itself.

The priority should be to fix:

- code issues
- packaging script issues
- workflow configuration issues

Until test packaging passes, production release should not continue.

### The production Release exists, but files are missing

That means the version already exists, but the release page is incomplete.

The right response is to repair the Release instead of creating another version immediately.

In practice, that means rerunning `manual-release-test` against the same production tag and uploading the rebuilt assets back to that same Release.

### The production package was released, but users cannot use it

If end users are affected—for example:

- installation fails
- the app does not open
- the app crashes immediately

The safer response is:

1. fix the issue on `main`
2. publish the next patch release

For example:

- `0.0.1` has a problem
- the fix is released as `0.0.2`

Unless there is a very specific reason, rewriting an already published production tag should not be the first choice.

### Local `dotnet build -c Release` fails before packaging starts

Check these first:

- the app project should not default to trim / ReadyToRun for ordinary Release builds
- MSBuild must resolve a concrete platform instead of falling back to AnyCPU
- if MSIX validation is not the current task, fix the build baseline before touching the release workflow

### Local MSIX build fails with certificate or signature errors

Check these in order:

- the certificate `Subject` must exactly match the `Publisher` in `App/Package.appxmanifest`
- `signtool.exe` must be installed and discoverable
- the generated `.cer` should be imported into the current user's **Trusted People** and **Trusted Root Certification Authorities** stores for local verification

If those conditions are not true, treat it as an environment or signing configuration issue first.

### Local EXE installer build fails immediately

Check these first:

- whether `ISCC.exe` exists (for example via Inno Setup 6)
- whether the source publish directory actually contains the built app files
- whether the resolved installer output path is writable

---

## Current release identity

The current release configuration uses:

- Product name: `AutoJS6 Visual Development Toolkit`
- Package identity: `space.terwer.autojs6devtools`
- Publisher: `CN=terwer`

If installer titles, package names, or publisher details ever look wrong, these values should be checked first.

---

## Files to inspect when deeper debugging is needed

- `.github/workflows/release-please.yml`
- `.github/workflows/manual-release-test.yml`
- `scripts/release/Set-AppReleaseMetadata.ps1`
- `scripts/release/New-CodeSigningCertificate.ps1`
- `scripts/release/Build-PortablePackage.ps1`
- `scripts/release/Build-InnoInstaller.ps1`
- `packaging/windows/autojs6-dev-tools.iss`
- `packaging/windows/ChineseSimplified.isl`
