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

## Test packaging / repair

Test packaging and repair both use:

- GitHub Actions workflow: `manual-release-test`

This flow covers the two most common needs.

### 1. Checking whether the package is still usable

Typical examples:

- the workflow was changed
- packaging scripts were changed
- a release is coming and ZIP / EXE / MSIX should be validated first

In that case, simply run `manual-release-test`.

The most common approach is:

- build from `main`
- use a normal test version
- do not upload to GitHub Release

That keeps all outputs in the current Actions run, where they can be downloaded and checked without affecting the public Release page.

### 2. Repairing an existing Release that is missing files

Typical examples:

- the official Release page already exists
- some ZIP / EXE / MSIX files are missing
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

- `1.0.1`
- `1.2.3`

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

MSIX is also produced, but because it currently requires trusting a certificate first, it is not the easiest first choice for most users.

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

- `1.0.1` has a problem
- the fix is released as `1.0.2`

Unless there is a very specific reason, rewriting an already published production tag should not be the first choice.

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
- `scripts/release/Build-MsixPackage.ps1`
- `packaging/windows/autojs6-dev-tools.iss`
- `packaging/windows/MSIX-INSTALL.md`
