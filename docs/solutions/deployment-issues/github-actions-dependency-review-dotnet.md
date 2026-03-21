---
title: "GitHub dependency review for .NET (NuGet) — playbook"
category: deployment-issues
tags: [github-actions, supply-chain, dependency-review, nuget, dotnet, kpatcher]
module: KPatcher
symptom: "No PR-time signal for vulnerable or disallowed transitive NuGet dependencies."
root_cause: "Dependency graph / dependency-review-action not wired into workflows; optional lockfiles missing."
---

## Symptom

Pull requests merge without automated comparison of **new or updated** dependencies against GitHub’s vulnerability and license data. For .NET, transitive packages may not appear reliably in the graph without **lockfiles** or explicit submission.

## Preconditions (repository)

1. **Dependency graph** enabled for the repo ([Dependency graph](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/about-the-dependency-graph)).
2. For **private** repos, **GitHub Advanced Security** may be required for full dependency review features ([About dependency review](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/about-dependency-review)).

## Working pattern (PR workflow)

Add a job or step that runs on **`pull_request`** (and optionally **`pull_request_target`** only if you understand the security tradeoffs — prefer **`pull_request`** for forks):

- **Permissions:** `contents: read` (minimum for `dependency-review-action` to read the comparison manifest).
- **Action:** [`actions/dependency-review-action`](https://github.com/actions/dependency-review-action) per [Configuring the dependency review action](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/configuring-the-dependency-review-action).

Example shape (adjust versions and policy inputs to org standards):

```yaml
permissions:
  contents: read

jobs:
  dependency-review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/dependency-review-action@v4
        # Optional: fail-on-severity, allow-licenses, deny-licenses, etc.
```

## .NET / NuGet specifics

- **Transitive fidelity:** Prefer **`packages.lock.json`** (restore with locked mode) so the graph reflects exact versions; without lockfiles, GitHub may infer fewer edges ([NuGet lock files](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#locking-dependencies)).
- **Monorepo:** Multiple `*.csproj` files are fine; ensure **restore** runs in CI so manifests are current before review (existing `dotnet restore` / build jobs help).
- **Complements (not duplicates):** **`dotnet restore` + NuGet audit** (SDK 6+) flags vulnerable packages at build time; dependency review adds **PR diff** context and policy gates ([Auditing packages](https://learn.microsoft.com/nuget/concepts/auditing-packages)).

## Prevention

- Document in **`AGENTS.md`** or contributor docs that PRs expect dependency review when enabled.
- Pair with **#9** release runbook items: **SBOM** (`sbom-tool`), **artifact attestations** — separate from PR dependency review ([Artifact attestations](https://docs.github.com/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations)).

## Cross-references

- Deepened plan: `docs/plans/2026-03-21-cli-layout-bundling-deepened.md` (seventh / tenth / **twelfth** pass — supply chain).
- Repo status: as of documentation, **no** `dependency-review-action` under `.github/workflows/` — this file is the **implementation checklist** for follow-up **#18**.
