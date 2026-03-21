---
title: "MSBuild PublishDir only applies under the Publish target"
category: deployment-issues
tags: [github-actions, msbuild, net48, dotnet-publish, kpatcher]
module: KPatcher
symptom: "Release zip or verify step finds empty or wrong folder; net48 artifacts missing from dist/build/..."
root_cause: "Invoking msbuild without /t:Publish runs the default Build target; PublishDir is ignored for output layout."
---

## Symptom

GitHub Actions (or local) sets `PublishDir=dist\build\net48\<rid>\` on **`msbuild`** but the expected **publish** layout never appears under that path (or archives are empty). **`dotnet publish`** is not used for the net48 matrix leg.

## Root cause

For SDK-style projects, **`PublishDir`** is consumed by the **`Publish`** target. The default **`msbuild`** entry target is **`Build`**, which writes to **`OutputPath`** / `bin` — not **`PublishDir`**. So a step that only sets **`PublishDir`** without **`/t:Publish`** does not produce a publish output at that location.

## Working fix

Use an explicit publish invocation:

```text
msbuild src/KPatcher/KPatcher.csproj /t:Publish ^
  /p:Configuration=Release ^
  /p:TargetFramework=net48 ^
  /p:RuntimeIdentifier=win7-x64 ^
  /p:SelfContained=false ^
  /p:PublishDir=dist\build\net48\win7-x64\
```

PR **`test-builds`** was aligned to the same pattern with **`PublishDir=dist\test-build\...`** so test output mirrors **release publish** semantics.

## Prevention

- In workflow docs and reviews, treat **`PublishDir`** as **Publish-only** unless the step explicitly runs **`/t:Publish`** or **`dotnet publish`**.
- Prefer **`dotnet publish`** for net* when the SDK is available; **`msbuild /t:Publish`** is appropriate for **net48** legs that already use **`microsoft/setup-msbuild`**.

## Cross-references

- Deepened plan: `docs/plans/2026-03-21-cli-layout-bundling-deepened.md` (fifth pass — net48 precision, follow-up #12 / #15).
- Related: `docs/solutions/deployment-issues/gha-pwsh-shell-syntax-mismatch.md` (shell choice for matrix `if` branches).
