---
title: "GitHub Actions: bash-style if under shell pwsh (invalid workflow script)"
category: deployment-issues
tags: [github-actions, pwsh, yaml, ci, dotnet-publish]
module: .github/workflows
symptom: "Release or PR workflow Build step fails to parse, or behaves unpredictably on windows-latest"
root_cause: "run: block used POSIX if/then/else/fi (or incomplete bash if) while steps[*].shell was pwsh — syntax must match the declared shell"
---

## Symptom

- **`build-all-platforms`** (or similar) **Build** step errors on **`windows-latest`** with PowerShell parse errors (e.g. missing `(` after `if`), or the step never runs the intended branch.
- Workflow sets **`shell: pwsh`** but the script uses **`if [ "$VAR" == "x" ]; then`** / **`else`** without valid **PowerShell** or complete **bash** grammar.

## Root cause

GitHub Actions runs the **`run`** script with the interpreter selected by **`jobs.<job_id>.steps[*].shell`**. **`pwsh`** does not accept **`if [ … ]; then`**. A half-bash block (e.g. **`if …; then` … `else`** without **`fi`**) is invalid for **bash** as well.

See: [Workflow syntax for GitHub Actions — `jobs.<job_id>.steps[*].shell`](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idstepsshell).

## Working fix (KPatcher)

Use **one dialect per step**:

- **Option A (recommended here):** **`shell: pwsh`** with native PowerShell:

```pwsh
if ("${{ matrix.framework }}" -eq "net48") {
  msbuild src/KPatcher/KPatcher.csproj `
    /p:Configuration=Release `
    ...
} else {
  dotnet publish ...
}
```

- **Option B:** Set **`shell: bash`** (e.g. Git-Bash on Windows) and use a **complete** **`if …; then … else … fi`** script.

Align **`test-builds.yml`** and **`build-all-platforms.yml`** so maintainers can copy/paste the same pattern.

## Verification

- Run the workflow on **`windows-latest`** at least once after the change; confirm both **net48** and **net9** branches execute.
- Optional: add **Verify publish output** (file presence + optional **`--help`** smoke) after **`dotnet publish`**.

## Prevention

- Code review checklist: **`shell:`** keyword matches **`run:`** syntax (**`pwsh`** vs **`bash`** vs **`cmd`**).
- Prefer **`pwsh`** on Windows for **`dotnet`**/MSBuild matrix jobs when the rest of the job is already PowerShell (line continuations with **`` ` ``**).

## Cross-references

- Deepened plan: **`docs/plans/2026-03-21-cli-layout-bundling-deepened.md`** (eighth pass).
- Bundled CLI MSBuild merge: [`kpatcher-publish-bundled-cli-tools-merge.md`](./kpatcher-publish-bundled-cli-tools-merge.md).
- Related: **`docs/solutions/debugging-patterns/ncsdecomp-lexer-pushback-java-parity.md`** (DeNCS lexer; separate domain).
