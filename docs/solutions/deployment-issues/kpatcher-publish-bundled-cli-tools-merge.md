---
title: "KPatcher PublishBundledCliTools — staging sidecars then merging into PublishDir"
category: deployment-issues
tags: [msbuild, dotnet-publish, kpatcher, kcompiler, ncsdecomp, bundling]
module: KPatcher
symptom: "Publishing KPatcher alone leaves kcompiler/NCSDecompCLI missing from output; or chained publish wipes KPatcher output"
root_cause: "Nested publishes must not target the final PublishDir directly; merge via intermediate stage folders"
---

## Symptom

- Release folder has **`KPatcher`** but not **`kcompiler`** / **`NCSDecompCLI`** when users expect an “all-in-one” directory.
- An attempt to **`Publish`** multiple exes **into the same `PublishDir` in sequence** without staging can **overwrite** or **wipe** the main app output.

## Root cause

**`dotnet publish`** / **`Publish`** target output is owned by **one** project per invocation. Publishing a sidecar **directly** to **`KPatcher`’s final `PublishDir`** as a second step risks clearing or conflicting with files from the first publish.

## Working pattern (KPatcher)

**`src/KPatcher/KPatcher.csproj`** defines **`PublishBundledCliTools`**:

- **`AfterTargets="Publish"`** — runs when **`KPatcher`** publish has finished.
- **`Condition="'$(TargetFramework)' == 'net9.0'"`** — **no** bundled merge for **`net48`** / other TFMs (sidecars stay absent unless you extend the condition).
- **Staging:** **`$(IntermediateOutputPath)/sidecar_kcompiler/`** and **`sidecar_ncsdecomp/`** — each receives an **`MSBuild` `Targets="Publish"`** with **`PublishDir`** set to **that stage root only** (not KPatcher’s final dir).
- **Merge:** **`Copy`** from each stage into **`_PublishDirNormalized`** using **`%(RecursiveDir)`** so subtree layout is preserved.

Sidecar publish properties are forwarded via **`_SidecarPublishProps`** (configuration, TFM, RID, self-contained, R2R, trim, **`UseAppHost=true`**) so the sidecars match the main app’s publish pivot.

## Expected layout (net9, typical CI)

- **KPatcher** may be **single-file** (`PublishSingleFile`); **sidecars** are usually **multi-file** outputs copied into the **same** folder — expect **duplicate managed DLL names** / native satellites; this is normal unless you isolate sidecars into subfolders.
- **Collision risk:** If two publishes bring the **same filename** with **different content**, **last copy wins** — treat as a release bug.

## Verification

- Local: **`dotnet publish`** **`KPatcher.csproj`** with **`-f net9.0`**, **`-r`**, and same self-contained flags as CI; confirm **`kcompiler`** / **`NCSDecompCLI`** apphosts beside **`KPatcher`**.
- CI: **`test-builds`** / **`build-all-platforms`** verify steps (presence, **`--help`** smoke) — see **`.github/workflows/*.yml`**.

## Prevention

- When adding a **third** bundled CLI, add another **stage dir** + **`MSBuild Publish`** + **`Copy`** — do not chain **`PublishDir`** to KPatcher’s final output without staging.
- When bumping **KPatcher** to **net10+**, extend the **`Condition`** (and sidecar TFMs) in lockstep.

## Cross-references

- Deepened plan: **`docs/plans/2026-03-21-cli-layout-bundling-deepened.md`**
- CI shell pitfalls: [`gha-pwsh-shell-syntax-mismatch.md`](./gha-pwsh-shell-syntax-mismatch.md)
- Microsoft: [MSBuild Copy task](https://learn.microsoft.com/visualstudio/msbuild/copy-task), [Well-known item metadata `RecursiveDir`](https://learn.microsoft.com/visualstudio/msbuild/msbuild-well-known-item-metadata)
