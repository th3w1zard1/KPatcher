---
title: Treat install logs as a flight recorder
date: 2026-05-23
category: architecture-patterns
module: KPatcher.Core.Patcher
problem_type: architecture_pattern
component: tooling
severity: low
applies_when:
  - "An install needs to be reconstructed after the fact"
  - "The raw install log is too noisy to share directly"
  - "CLI and UI should expose the same diagnostic story"
related_components:
  - KPatcher.Core.Logger
  - KPatcher.Core.Patcher
  - KPatcher.UI.ViewModels
tags:
  - install-log
  - diagnostics
  - parity
  - supportability
---

# Treat install logs as a flight recorder

## Context

KPatcher already writes an install log during `ModInstaller` runs and streams patch activity into it. The gap was not logging itself; it was having a compact, shareable record that preserves what happened without asking users to reconstruct the run from scattered notes.

## Guidance

Keep the existing raw install log as the low-level source of truth, and layer a concise install record on top of it when you need supportability. Capture the install context, the resolved inputs, the patch order, warnings or errors, and the final outcome, but avoid turning the record into a second verbose debug dump.

## Why This Matters

A shareable record makes parity issues and support requests cheaper to diagnose. If the record becomes a raw log clone, users still have to sift through noise and maintainers lose the benefit of a compact evidence bundle.

## When to Apply

- When install behavior must be explainable after the fact
- When support needs a single artifact instead of a transcript
- When the same diagnostic story should work in both the UI and CLI

## Examples

- `src/KPatcher.Core/Patcher/ModInstaller.cs` already initializes `InstallLogWriter` and writes the install header, start, progress, and final status.
- `src/KPatcher.UI/ViewModels/MainWindowViewModel.cs` mirrors install-related logging into the UI, so the recorder should share the same story rather than inventing a separate one.
- Before: `installlog.txt` contains every message, but nothing distinguishes the minimal evidence a maintainer needs.
- After: the recorder surfaces a compact install summary built from the same install stream, with the raw log still available for deeper inspection.

## Related

- `src/KPatcher.Core/Logger/InstallLogWriter.cs`
- `src/KPatcher.Core/Patcher/ModInstaller.cs`
- `src/KPatcher.UI/ViewModels/MainWindowViewModel.cs`
