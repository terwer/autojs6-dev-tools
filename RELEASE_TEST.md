# Release Test Documentation Entry (Start Here)

If you are currently working on **GitHub Actions release validation, package verification, release repair, or final pre-release checks**, start with this page.

## 1. Read the runbook first

- File: `manual.md`
- Purpose: explains how to run **manual-release-test** and **release-please**, what to check in each step, and how to judge success

> Note: `manual.md` is currently written in Chinese-first form.

## 2. Then read the validation checklist

- File: `checklist.md`
- Purpose: defines whether the package is actually ready for release from the end-user perspective

> Note: `checklist.md` is currently written in Chinese-first form.

## 3. If GitHub push / proxy / workflow visibility is the problem

- File: `PROXY.md`
- Purpose: explains why workflows may exist locally but still not appear on GitHub, and how GitHub / proxy / push should be configured

## 4. If you are editing workflows or release scripts

- File: `DEVELOPMENT.md`
- Purpose: explains the project release paths, workflow responsibilities, and why proven working references must be followed before they are refactored

---

## Shortest path

### I want to run `manual-release-test`

Read:

1. `manual.md`
2. `checklist.md`

### I want to fix GitHub push / Actions not showing

Read:

1. `PROXY.md`
2. `manual.md`

### I want to modify workflow token / secret / env mapping

Read:

1. `DEVELOPMENT.md`

---

## One-line memory aid

### The release-test doc entry is:

- `RELEASE_TEST.md` ← start here
- `manual.md`
- `checklist.md`

