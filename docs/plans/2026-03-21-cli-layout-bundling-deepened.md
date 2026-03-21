# KPatcher / KCompiler / NCSDecomp CLI and bundling — **Deepened plan**

## Enhancement Summary

**Deepened on:** 2026-03-21  
**Second pass:** 2026-03-21 — `repo-research-analyst` + `best-practices-researcher` (Microsoft Learn synthesis); **`docs/solutions/`** now includes compound write-ups (see **ninth pass** learnings list).  
**Third pass:** 2026-03-21 — same agents: repo CI/workflows vs plan claims; Microsoft Learn + GitHub docs for **release hardening** (signing, matrix publish, SBOM).  
**Fourth pass:** 2026-03-21 — workflow **exact** `dotnet publish` args, zip naming, absence of `Directory.Build.*`; **Linux desktop** + **MSBuild `Copy`/`RecursiveDir`** guidance (Avalonia / Flatpak / AppImage).  
**Fifth pass:** 2026-03-20 — `repo-research-analyst` + `best-practices-researcher`: **CI vs `publish_release.ps1`** contract, net48 **`msbuild` target precision**, workflow property inventory; **R2R / signing / reproducible builds / AV heuristics / macOS notarization** (docs links).  
**Sixth pass:** 2026-03-20 — `repo-research-analyst` + `best-practices-researcher`: **full re-verify** plan vs `KPatcher.csproj` / GHA workflows / `KEditChanges.NET` / `AGENTS.md`; **Microsoft Learn** — `AfterTargets="Publish"` on a **custom** target (avoid fragile `AfterPublish`), **`PrepareForBundle`** / **`GenerateSingleFileBundle`** ordering, **flat `PublishDir` collisions** across nested publishes, **CI assert + smoke** each apphost, **`AppContext.BaseDirectory`** vs **`Assembly.Location`** for loose files beside single-file host.  
**Seventh pass:** 2026-03-21 — `repo-research-analyst` + `best-practices-researcher`: **re-verify** workflows + **`AGENTS.md`** (no new drift vs sixth pass); **`test-builds` verify** runs for **all** matrix rows (**net48** + **net9**); **`continue-on-error: true`** on tests; **GitHub supply chain** — dependency review, artifact attestations, SBOM + attest; **CI verify patterns** — per-OS shells, **`AssemblyName`** = apphost basename, Unix **executable** bit, don’t assert “single file only” unless properties guarantee it.  
**Eighth pass:** 2026-03-21 — `repo-research-analyst` + `best-practices-researcher`: **`build-all-platforms.yml` Build** used **bash `if`** under **`shell: pwsh`** with **no `fi`** — **invalid** for both shells (GitHub docs: **syntax must match declared `shell`**). **Fix:** PowerShell **`if () { } else { }`** like **`test-builds.yml`**. **GHA shell defaults** table — Windows **`pwsh`**, use **`shell: bash`** explicitly for bash scripts. **Implemented in repo:** verify step on **`build-all-platforms`** + net9 sidecar asserts in **`test-builds`**; **`AGENTS.md`** canonical publish + bundled apphost rows; compound learning **`docs/solutions/debugging-patterns/ncsdecomp-lexer-pushback-java-parity.md`**.  
**Ninth pass:** 2026-03-21 — `repo-research-analyst` + `best-practices-researcher`: **post-implementation** audit — sidecar verify + pwsh Build + **`AGENTS`** rows **confirmed**; **remaining gaps** = parity audit, ~~net48 **`/t:Publish`**~~ (**done** — see **eleventh pass**), supply chain, runbook, **`docs/solutions`** growth (lexer + GHA shell + publish-target doc). **Implemented:** **`--help` smoke**; compound doc for **pwsh/bash mismatch**.  
**Tenth pass:** 2026-03-21 — `repo-research-analyst` + `best-practices-researcher`: **stale-plan cleanup** — Key improvements **#11–#12** and **third-pass “Verify gap”** superseded by **eighth–ninth pass** CI; **`docs/solutions/`** grew to **four** deployment/debugging articles (merge + GHA shell + **`PublishDir`/`Publish`** + lexer parity). **Dependency review:** enable **Dependency graph** (repo settings); **`dependency-review-action`** on **`pull_request`** with **`permissions: contents: read`**; **NuGet lockfiles optional** for graph but **recommended** for transitive vuln fidelity; **private repos** may need **GitHub Advanced Security** — [About dependency review](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/about-dependency-review), [Configuring the dependency review action](https://docs.github.com/en/code-security/supply-chain-security/understanding-your-software-supply-chain/configuring-the-dependency-review-action), [Dependency graph data](https://docs.github.com/en/code-security/concepts/supply-chain-security/dependency-graph-data).  
**Eleventh pass:** 2026-03-20 — **`/work` + compound:** net48 **`msbuild /t:Publish`** in **`build-all-platforms.yml`** and **`test-builds.yml`** ( **`PublishDir`** honored); **`test-builds`** net48 aligned with release **publish** semantics (no **`OutputPath`-only** build). Compound: [`deployment-issues/msbuild-publishdir-requires-publish-target.md`](../solutions/deployment-issues/msbuild-publishdir-requires-publish-target.md).  
**Twelfth pass:** 2026-03-21 — **Repo audit:** `.github/workflows` still has **no** `dependency-review-action` / `attest` / SBOM steps — supply chain remains **documentation + follow-up #18**. **Compound:** playbook [`deployment-issues/github-actions-dependency-review-dotnet.md`](../solutions/deployment-issues/github-actions-dependency-review-dotnet.md). **Plan hygiene:** third-pass **“Verify gap”** struck through; **`test-builds`** touchpoint table corrected (verify covers **net9** sidecars + **`--help`**).  
**Base plan:** [.cursor/plans/cli_layout_and_bundling_2697e2b6.plan.md](../../.cursor/plans/cli_layout_and_bundling_2697e2b6.plan.md) (unchanged)  

**Section manifest (research scope)**

| # | Section | Research focus |
|---|---------|------------------|
| 1 | Layering / folders | .NET solution structure, Core vs UI vs host |
| 2 | Headless CLIs | HoloPatcher parity, shared library entry points |
| 3 | Publish bundling | MSBuild `AfterTargets Publish`, staging, collision avoidance |
| 4 | KEditChanges umbrella | Subcommand host, SDK identity, single-file |
| 5 | Documentation / CI | AGENTS.md, publish smoke, artifacts layout |
| 6 | Release hardening | Code signing, GHA matrix + RID, SBOM / supply chain |
| 7 | Linux desktop + Copy metadata | Avalonia Linux layout, Flatpak/AppImage, MSBuild recursive copy |
| 8 | Release engineering | CI vs local zip naming; R2R/trim/AOT inventory; signing, reproducibility, macOS |

### Key improvements (all passes)

1. **Publish merge pattern** — staging + `Copy` beats chained `Publish` to one `PublishDir` (avoids wiping KPatcher output).
2. **Testability** — `InternalsVisibleTo`, `HoloPatcherCliTests` without launching Avalonia.
3. **SDK / NuGet** — umbrella as **`keditchanges-cli`** to avoid ambiguous graph identity.
4. **Framework matrix** — `PublishBundledCliTools` is **`net9.0` only** until sidecar TFMs align.
5. **Official docs alignment** — solution-level `--output` pitfalls, `IsPublishable`, artifacts output, single-file CLI caveats (see appendix).
6. **CI truth vs plan** — net48 zips without sidecars; ~~verify-step gap~~ **closed** (eighth–ninth pass); single-file + multi-file merge clarification; corrected staging/`PublishDir` wording.
7. **Release hardening track** — signing, GHA matrix/RID, SBOM and supply-chain links (third pass section + appendix rows).
8. **Workflow-grounded publish recipe** — Release/PR net9 jobs use **self-contained** + **PublishSingleFile** + **IncludeNativeLibrariesForSelfExtract** + **PublishReadyToRun** (matches `build-all-platforms.yml` / `test-builds.yml`).
9. **Linux distribution + Copy pitfalls** — Avalonia FHS-style layout, `.desktop` spec, Flatpak/AppImage caveats; MSBuild **`%(RecursiveDir)`** for subtree-preserving merges.
10. **CI truth, refined** — Workflows set **no** `PublishTrimmed` / `PublishAot`; **`ci.yml` packs `KCompiler.Core` + `KCompiler.NET`**; **`publish_release.ps1` uses pubxml + `dist\<rid>.zip`**, not GHA’s `KPatcher-<ver>-<platform>-<arch>.zip`.
11. **net48 publish target** — **`build-all-platforms`** and **`test-builds`** net48 legs now use **`msbuild /t:Publish`** with **`PublishDir`** so output matches **`dist/build/...`** / **`dist/test-build/...`** (see [`deployment-issues/msbuild-publishdir-requires-publish-target.md`](../solutions/deployment-issues/msbuild-publishdir-requires-publish-target.md)). ~~**bash `if` under `pwsh`**~~ **fixed** (eighth pass).
12. ~~**Sixth-pass CI gap**~~ **Resolved (eighth–ninth pass)** — both workflows assert **net9** sidecar apphosts, sizes, **`chmod +x`**, and **`--help` exit 0**; historical wording kept in older “New considerations” sections is **superseded**.
13. **Docs vs release flags** — **`AGENTS.md`** retains a **minimal** publish row **and** adds **canonical CI-matching** + bundled apphost + zip rows (**follow-up #10 done**); contributors choosing minimal **`-f net9.0`** still differ from release matrix unless they add **`-r`** / self-contained / single-file props.
14. **Verify step semantics** — **`test-builds.yml`** “Verify build output” runs for **every** matrix cell (**net48** and **net9**). Any future **`kcompiler` / `NCSDecompCLI`** assertions must be **scoped to net9** (or rows where `PublishBundledCliTools` runs); net48 rows **must not** require bundled sidecars.
15. **Tests vs publish signal** — **`test-builds.yml`** may run tests with **`continue-on-error: true`** — green verify/publish with a **red** test job is possible; treat as **policy** (document intent) or tighten.
16. **Supply chain (GHA)** — Complements third-pass hardening: **dependency review** on PRs, **artifact attestations** (`actions/attest`) on shipped zips/installers, **SBOM** generation + optional SBOM attestation (see seventh-pass links).
17. **Cross-platform CI asserts** — Use **explicit** `shell:` where checks differ; assert apphost names from each project’s **`AssemblyName`**; on Linux/macOS use **`test -x`** (not only **`-f`**); allow **documented** extra native/satellite files beside a single-file host.
18. **`build-all-platforms` Build was broken** — Eighth-pass review: **`if [ … ]; then`** + **`else`** without **`fi`**, under **`pwsh`**, is a **parse error**; seventh pass “replay to see if it parses” understates severity — **must** rewrite in PowerShell or switch **`shell:`** ([Workflow syntax — `jobs.<job_id>.steps[*].shell`](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idstepsshell)).
19. **Compound knowledge** — NCSDecomp **lexer `PushBack`** Java parity documented under **`docs/solutions/debugging-patterns/`** for future deepen-plan **`docs/solutions/`** scans.
20. **CLI `--help` smoke in CI** — After apphost presence checks, run **`& $path @('--help')`** and assert **`$LASTEXITCODE -eq 0`** immediately ([about_Automatic_Variables](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_automatic_variables)); on Linux/macOS **`chmod +x`** before invoke if the execute bit may be missing ([single-file / extract](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview)). **KPatcher** / **kcompiler** / **NCSDecompCLI** all support **`--help`** and exit **0** when help is requested.
21. **Supply chain playbook (twelfth pass)** — **Dependency review** is **not** in YAML yet; when implementing **follow-up #18**, use [`github-actions-dependency-review-dotnet.md`](../solutions/deployment-issues/github-actions-dependency-review-dotnet.md) (graph + `dependency-review-action` + optional **NuGet lockfiles**).

### Learnings from `docs/solutions/` (applied to plan narrative)

| File | Relevance to this plan |
|------|-------------------------|
| [`debugging-patterns/ncsdecomp-lexer-pushback-java-parity.md`](../solutions/debugging-patterns/ncsdecomp-lexer-pushback-java-parity.md) | DeNCS port / NCS CLI quality — not bundling-specific, but documents **lexer parity** risk when changing **`NCSDecomp.Core`**. |
| [`deployment-issues/gha-pwsh-shell-syntax-mismatch.md`](../solutions/deployment-issues/gha-pwsh-shell-syntax-mismatch.md) | **Direct:** **`shell: pwsh`** must pair with **PowerShell** scripts; cross-links eighth pass + workflows. |
| [`deployment-issues/kpatcher-publish-bundled-cli-tools-merge.md`](../solutions/deployment-issues/kpatcher-publish-bundled-cli-tools-merge.md) | Staging + **`Copy`** merge for **`PublishBundledCliTools`**; avoid shared **`PublishDir`** for nested publishes. |
| [`deployment-issues/msbuild-publishdir-requires-publish-target.md`](../solutions/deployment-issues/msbuild-publishdir-requires-publish-target.md) | **`PublishDir`** requires **`/t:Publish`** (or **`dotnet publish`**); net48 GHA legs. |
| [`deployment-issues/github-actions-dependency-review-dotnet.md`](../solutions/deployment-issues/github-actions-dependency-review-dotnet.md) | **PR supply chain:** dependency graph + **`dependency-review-action`** checklist for .NET/NuGet; repo **YAML not yet** wired (twelfth pass). |

### New considerations (second pass)

- **Publish matrix:** Document behavior when KPatcher adds `net10.0` while bundle condition stays `net9.0` only — extend condition + sidecar TFMs or state “bundle only when publishing `-f net9.0`.”
- **Optional fourth binary:** `keditchanges-cli` is **not** merged into KPatcher publish today (lean default); product decision to add a third nested `Publish` + copy.
- **Dependency flattening:** Flat `Copy` of two sidecars into one folder — audit duplicate DLL names/versions on upgrade (Microsoft warns about single shared output dirs for solutions).
- **Solution hygiene:** `KPatcher.sln` has an unused **`src` solution folder** (no nested projects); nest projects or remove folder for clarity.
- **Artifacts output (.NET 8+):** Optional `UseArtifactsOutput` for predictable per-project publish trees before merge ([Artifacts output layout](https://learn.microsoft.com/dotnet/core/sdk/artifacts-output)).

### New considerations (third pass — CI / docs accuracy)

- **net48 zips:** `PublishBundledCliTools` is **`net9.0` only** — CI jobs that publish **net48** KPatcher (e.g. `build-all-platforms.yml`) **do not** run the merge target; release artifacts may be **GUI-only** for that TFM unless documented otherwise.
- ~~**Verify gap:** `test-builds.yml` checks **KPatcher** apphost only — **no** assertion that **`kcompiler`** / **`NCSDecompCLI`** landed in `PublishDir` after `PublishBundledCliTools`.~~ **Superseded** (eighth–ninth pass + **`--help`** smoke); see Key improvements **#12**, **#20**.
- **Single-file KPatcher vs multi-file sidecars:** Release workflows may set **`PublishSingleFile`** on **KPatcher** while nested sidecar publishes default to **multi-file** output copied into the same folder — document expected layout (duplicate managed DLLs, native satellites) for support.
- **Staging vs final `PublishDir`:** Nested `MSBuild` `Publish` uses **`PublishDir` = stage roots** (`sidecar_*`), **not** KPatcher’s final `PublishDir`; the **Copy** task performs the merge. Wording “pass absolute final PublishDir to children” would be **incorrect** for this implementation.
- **AGENTS minimal publish example:** A bare `dotnet publish -f net9.0` without **`-r` / `--self-contained`** differs from **release** matrix jobs; add a **canonical** one-liner matching `build-all-platforms.yml` when documenting releases.

### New considerations (fourth pass — workflows verified)

- **Canonical net9 publish (KPatcher)** — `dotnet publish src/KPatcher/KPatcher.csproj -c Release -f net9.0 -r <rid> --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o dist/build/net9.0/<rid>/` (adjust `-o` to match workflow). **`test-builds.yml`** uses the same property set for its matrix subset; **`build-all-platforms.yml`** covers more RIDs (`win-*`, `linux-*`, `osx-*`).
- **net48** — **`msbuild /t:Publish`** with `SelfContained=false` and `PublishDir=dist\build\net48\<rid>\` (not `dotnet publish` in `build-all-platforms`). **`PublishBundledCliTools` does not run** (net9-only condition). *(Historical: fifth pass noted missing **`/t:Publish`** — **fixed** in workflows + compound doc.)*
- **net48 test vs release:** **`test-builds`** net48 now uses the same **`PublishDir`** semantics as **`build-all-platforms`** (both **`/t:Publish`**). *(Historical: fourth pass **`OutputPath`** vs **`PublishDir`** mismatch — **resolved**.)*
- **No repo-wide Publish imports:** There is **no** `Directory.Build.props` / `Directory.Build.targets` — behavior is **project + workflow** only.
- **Actions do not publish** `NCSDecomp.UI` or `KEditChanges.NET` — only **`KPatcher.csproj`** is `dotnet publish`’d in workflows; KCompiler **NuGet pack** is separate in `ci.yml`.
- **Release zip names:** `KPatcher-<version>-<platform>-<arch>.zip` (e.g. `win` + `x64`), **not** raw RID strings — align docs/examples with `build-all-platforms.yml` artifact naming.

### New considerations (fifth pass — repo + workflows re-verified)

- **`build-all-platforms.yml` net9** — Only extra publish properties: **`PublishSingleFile`**, **`IncludeNativeLibrariesForSelfExtract`**, **`PublishReadyToRun`** (plus matrix **`SelfContained`**). **No** `PublishTrimmed`, **`PublishAot`**, **`EnableCompressionInSingleFile`** in workflow. **`KPatcher.csproj`** may set **`IncludeNativeLibrariesForSelfExtract`** globally (duplicated in CLI for clarity).
- **`build-all-platforms.yml` net48** — `msbuild /t:Publish` with **`PublishDir=dist\build\<tfm>\<rid>\`** — **implemented** (replaces fifth-pass “no `/t:Publish`” finding).
- **`upload-artifact`:** `name` is **`KPatcher-<version>-<platform>-<arch>`** (no `.zip` in the artifact **name**); **`path`** is the `.zip` file under `dist/`.
- **`publish_release.ps1`** — **`dotnet publish /p:PublishProfile=...`** from **`Properties/PublishProfiles`**; archives as **`dist\<rid>.zip`** — **different contract** from GitHub Actions naming; document both in AGENTS / release runbook.
- **`publish_release_standalone.ps1`** — Defaults to **`KOTORModSync.GUI`** / KOTOR Mod Sync branding — **not** KPatcher unless reparameterized; do not treat as canonical KPatcher local release without qualification.
- **Shell hazard (historical):** `build-all-platforms` once mixed **bash `if`** under **`shell: pwsh`** — **fixed** (eighth pass); see [`gha-pwsh-shell-syntax-mismatch.md`](../solutions/deployment-issues/gha-pwsh-shell-syntax-mismatch.md).

### New considerations (sixth pass — repo + Learn synthesis)

- **Re-verified (unchanged):** `PublishBundledCliTools` (`net9.0` only), staging + `Copy` with `%(RecursiveDir)`, sidecar project paths, `keditchanges-cli` assembly name, `ci.yml` KCompiler pack scope, zip/artifact naming — all **match** prior plan sections.
- ~~**Dual verify gap:** Treat **`build-all-platforms.yml`** the same as **`test-builds.yml`** for bundle regression…~~ **Superseded** (eighth–ninth pass + **`--help`**); both pipelines now assert sidecars on **net9**.
- **MSBuild hook pattern:** Prefer a **uniquely named** target with **`AfterTargets="Publish"`** (not redefining built-ins); use **`DependsOnTargets`** for your own chains. Community/SDK edge cases make **`AfterPublish`** timing less dependable than **`Publish`** completion — align any future signing/bundle hooks with **`PrepareForBundle`** → **`GenerateSingleFileBundle`** when mutating the single-file host ([Single-file overview](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview), [Extend the build process](https://learn.microsoft.com/visualstudio/msbuild/how-to-extend-the-visual-studio-build-process)).
- **Flat-folder collision discipline:** Merging **multiple self-contained** publishes into one directory risks **last-writer-wins** on shared runtime files; keep **RID / self-contained / TFM** aligned across nested publishes or isolate sidecars (subfolders / artifacts layout) if collisions appear ([Deploying .NET apps](https://learn.microsoft.com/dotnet/core/deploying/)).
- **Artifacts output (optional):** **`UseArtifactsOutput`** / **`--artifacts-path`** remains a structured alternative to a single shared **`PublishDir`** when scaling CI matrix uploads ([Artifacts output layout](https://learn.microsoft.com/dotnet/core/sdk/artifacts-output)).
- **Runtime path assumptions:** Code or tests that rely on **`Assembly.Location`** for files next to a **single-file** host may break; prefer **`AppContext.BaseDirectory`** for loose sidecars and satellites ([Single-file overview](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview)).
- **AGENTS vs GHA:** Sixth pass **reconfirmed** drift — minimal local publish in **`AGENTS.md`** vs full matrix properties in workflows; closing this is **docs-only** unless maintainers want `publish_release.ps1` to match GHA flag-for-flag.

### Research Insights (sixth pass — best practices, condensed)

**Best practices**

- Mirror **release** publish flags in **CI** (`-c`, `-f`, `-r`, self-contained, `PublishSingleFile`, native extract, R2R) so artifacts match what users download ([`dotnet publish`](https://learn.microsoft.com/dotnet/core/tools/dotnet-publish)).
- After publish, **assert by name** each expected apphost and critical loose native payloads; run a **smoke** (`--help`, exit code) from the **published directory** root.

**Performance / layout**

- **`ExcludeFromSingleFile`** + **`CopyToPublishDirectory`** for content that must stay beside the host; understand **`IncludeNativeLibrariesForSelfExtract`** and extraction dirs / **`DOTNET_BUNDLE_EXTRACT_BASE_DIR`** when tuning startup I/O ([Single-file overview](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview)).

**References (added this pass)**

| Topic | URL |
|-------|-----|
| Extend the build process (`AfterTargets`, custom targets) | https://learn.microsoft.com/visualstudio/msbuild/how-to-extend-the-visual-studio-build-process |
| `dotnet publish` | https://learn.microsoft.com/dotnet/core/tools/dotnet-publish |
| Deploying overview (output layouts, collisions) | https://learn.microsoft.com/dotnet/core/deploying/ |
| Single-file (bundle pipeline, hooks, extraction) | https://learn.microsoft.com/dotnet/core/deploying/single-file/overview |
| Artifacts output layout | https://learn.microsoft.com/dotnet/core/sdk/artifacts-output |
| Single-file design (history, edge cases) | https://github.com/dotnet/designs/blob/main/accepted/2020/single-file/design.md |

### New considerations (seventh pass — workflows + supply chain)

- **No new repo drift** vs sixth pass: bundle target, **`test-builds`** / **`build-all-platforms`** shape, **`AGENTS.md`** minimal publish — **unchanged**; ~~pwsh + bash **`if`** hazard~~ **fixed** (eighth pass); ~~sixth-pass verify gaps~~ **closed** (eighth–ninth pass).
- **Verify runs on net48 too:** Sidecar file asserts belong behind a **net9** (or TFM) condition so net48 jobs keep checking only **KPatcher** apphost + layout expectations without bundled CLIs. *(Still true.)*
- ~~**Operational validation:** One-time **replay** … bash-style **`if`** under **`pwsh`**~~ **Obviated** (Build rewritten in PowerShell, eighth pass).
- ~~**test-builds vs release net48:** … **`OutputPath`** vs **`PublishDir`**~~ **Resolved** (eleventh pass — both use **`msbuild /t:Publish`** + **`PublishDir`**).
- **Supply chain depth:** Add **dependency review** to PR workflow when dependency graph is enabled; use **artifact attestations** for release binaries; generate **SPDX/CycloneDX** SBOM (e.g. Microsoft **sbom-tool**) and attach or attest per GitHub docs (links below).

### Research Insights (seventh pass — CI verify + supply chain, condensed)

**CI verification**

- Prefer **one publish root per matrix cell**; include **`matrix.rid`** in paths and **cache keys** ([RID catalog](https://learn.microsoft.com/dotnet/core/rid-catalog)).
- **Windows:** `Test-Path …\KPatcher.exe` (etc.); **Linux/macOS:** extensionless apphost, **`[ -x file ]`** for executability.
- Single-file hosts may still have **sibling native/runtime files** — assertions should match [Single-file deployment](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview), not “exactly one file” unless fully pinned.

**Supply chain (official)**

| Topic | URL |
|-------|-----|
| About dependency review | https://docs.github.com/code-security/supply-chain-security/understanding-your-software-supply-chain/about-dependency-review |
| Configuring dependency review action | https://docs.github.com/code-security/supply-chain-security/understanding-your-software-supply-chain/configuring-the-dependency-review-action |
| Artifact attestations (provenance) | https://docs.github.com/actions/how-tos/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds |
| SBOM attestation (GitHub) | https://docs.github.com/actions/how-tos/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds#generating-an-attestation-for-a-software-bill-of-materials-sbom |
| .NET + GitHub Actions overview | https://learn.microsoft.com/dotnet/devops/github-actions-overview |
| Caching (setup-dotnet / workflows) | https://docs.github.com/actions/using-workflows/caching-dependencies-to-speed-up-workflows |

**Optional: workload cache** — Cache **`~/.dotnet/workloads`** with OS-specific keys; **`enableCrossOsArchive: false`**; run **`dotnet workload restore`** on miss ([`dotnet workload install`](https://learn.microsoft.com/dotnet/core/tools/dotnet-workload-install)).

### New considerations (eighth pass — shell correctness + implemented CI/docs)

- **`build-all-platforms` Build:** Prior YAML used **`if [ … ]; then` / `else`** with **no `fi`** and **`shell: pwsh`** — **not valid** bash or PowerShell. **Fixed:** native PowerShell **`if () { } else { }`**, matching **`test-builds.yml`** ([workflow `shell`](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idstepsshell)).
- **Release verify:** **`build-all-platforms.yml`** now runs **Verify publish output** after Build — **`KPatcher`** apphost + (**net9 only**) **`kcompiler`** / **`NCSDecompCLI`** with correct **`.exe`** suffix on Windows.
- **PR verify:** **`test-builds.yml`** verify step extended for **`net9.0`** matrix rows — same sidecar apphost checks; **`continue-on-error: true`** on tests **documented** inline in YAML.
- **`AGENTS.md`:** **Canonical CI-matching publish** row + **bundled apphost names** per OS + **zip naming** note.
- **Historical plan bullets** (e.g. fifth pass “Shell hazard”, third pass “Verify gap”, sixth pass “Dual verify gap”) describe **pre-fix** or **pre-superseded** state; treat **twelfth pass** **Key improvements** + **strikeouts** in older sections as **source of truth** for CI.

### Research Insights (eighth pass — GHA shell, condensed)

- Declared **`shell:`** must match script syntax; default on **Windows** hosted runners is **`pwsh`** — use **`shell: bash`** only when intentionally using Git-Bash-style scripts ([workflow syntax — shell](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idstepsshell)).
- **`dotnet publish`** commands are shell-agnostic; **conditionals and env** are not — prefer **one dialect per step** ([Building and testing .NET](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net)).

### New considerations (ninth pass — repo audit + `docs/solutions` + help smoke)

- **Confirmed in repo:** **`build-all-platforms`** / **`test-builds`** verify **KPatcher** + (**net9**) **`kcompiler`** / **`NCSDecompCLI`**; **Build** uses valid **PowerShell** under **`shell: pwsh`**; **`AGENTS.md`** lists canonical publish + bundled names + zip naming.
- **Open (unchanged):** HoloPatcher **parity audit**; **supply chain** actions (**dependency review**, **attestations**, SBOM); **release runbook** hardening; **`publish_release.ps1`** vs GHA zip names; **solution folder** cleanup in **`.sln`**. ~~**net48 `/t:Publish`**~~ — **done** (eleventh pass + [`msbuild-publishdir-requires-publish-target.md`](../solutions/deployment-issues/msbuild-publishdir-requires-publish-target.md)).
- **`docs/solutions/`:** **Five** compound-backed articles (under **`docs/solutions/`**) — future **`/deepen-plan`** passes should **`glob docs/solutions/**/*.md`** and merge frontmatter **`tags`** into research.
- **`--help` smoke:** Implemented in both workflows’ verify steps for every **required** apphost (**net48** = **KPatcher** only; **net9** = main + sidecars). **Avalonia / DISPLAY:** **`Program.Main`** handles **`--help`** before **GUI** bootstrap — suitable for headless Linux runners; regressions that touch **Avalonia** before parsing args would need **Xvfb** or similar ([GitHub-hosted runners](https://docs.github.com/en/actions/using-github-hosted-runners/using-github-hosted-runners/about-github-hosted-runners)).

### Research Insights (ninth pass — pwsh exit codes + single-file, condensed)

- Read **`$LASTEXITCODE`** immediately after the native apphost; do not interleave cmdlets that overwrite it ([about_Automatic_Variables](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_automatic-variables?view=powershell-7.5)).
- Single-file first run may trigger **extract** — allow time or set **`DOTNET_BUNDLE_EXTRACT_BASE_DIR`** if agents share temp ([single-file overview](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview)).
- Use **`shell: pwsh`** consistently on matrix legs when scripting with **`$IsWindows`** / **`$LASTEXITCODE`** so Linux jobs do not default to **bash** and diverge ([workflow syntax — shell](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idstepsshell)).

### New considerations (twelfth pass — supply chain status + doc cleanup)

- **Workflow grep:** No **`dependency-review-action`**, **`attest`**, or **SBOM** generation in **`.github/workflows/`** yet — seventh/tenth pass links remain the **design** reference; implementation = **follow-up #18** + [`github-actions-dependency-review-dotnet.md`](../solutions/deployment-issues/github-actions-dependency-review-dotnet.md).
- **Build vs Publish (net48):** Eleventh pass **`/t:Publish`** fix is the canonical “**`PublishDir` needs Publish**” story; keep [`msbuild-publishdir-requires-publish-target.md`](../solutions/deployment-issues/msbuild-publishdir-requires-publish-target.md) linked from **`AGENTS.md`** / workflow comments if maintainers reintroduce **`msbuild`** without **`/t:Publish`**.
- **Historical sections:** Third/sixth/seventh passes that mention “verify gap” or “dual verify gap” are **historical** unless explicitly ~~struck~~ or pointed at **tenth pass** “source of truth” note (line in eighth pass **New considerations**).

---

## Repo research synthesis (repo-research-analyst)

**Implementation map (verified paths)**

| Concern | Location |
|---------|----------|
| Bundle target | [`src/KPatcher/KPatcher.csproj`](../../src/KPatcher/KPatcher.csproj) — `PublishBundledCliTools`, `AfterTargets="Publish"`, `Condition` on `net9.0` |
| Staging | `$(MSBuildProjectDirectory)` + `$(IntermediateOutputPath)` → `sidecar_kcompiler\`, `sidecar_ncsdecomp\` |
| Sidecar props | `_SidecarPublishProps`: Configuration, TargetFramework, RuntimeIdentifier, SelfContained, PublishReadyToRun, PublishTrimmed, `UseAppHost=true` |
| HoloPatcher CLI | [`src/KPatcher/HoloPatcherCli.cs`](../../src/KPatcher/HoloPatcherCli.cs), [`src/KPatcher/Program.cs`](../../src/KPatcher/Program.cs) |
| Shared NCS CLI | [`src/NCSDecomp.Core/NcsDecompCli.cs`](../../src/NCSDecomp.Core/NcsDecompCli.cs); host [`src/NCSDecomp.NET/Program.cs`](../../src/NCSDecomp.NET/Program.cs) |
| Umbrella | [`src/KEditChanges.NET/`](../../src/KEditChanges.NET/) — **not** in `PublishBundledCliTools` |
| Tests | [`src/KPatcher.Tests/HoloPatcherCliTests.cs`](../../src/KPatcher.Tests/HoloPatcherCliTests.cs) |

**Gaps vs brainstorm open questions**

- Rename **`keditchanges-cli` → `keditchanges.exe`:** still open (post-publish rename vs assembly/project rename).
- **`parse_known_args` parity:** behavioral follow-up, not MSBuild.
- **ChangeEdit parity:** KEditChanges library still placeholder beyond bundling scope.

**CI / scripts touchpoints (third pass)**

| Area | Path |
|------|------|
| Multi-platform publish + zip | `.github/workflows/build-all-platforms.yml` |
| Smaller matrix + verify | `.github/workflows/test-builds.yml` — **KPatcher** + (**net9**) sidecars + **`--help`** smoke; tests may **`continue-on-error`** |
| Tests / analyzers / KCompiler pack | `.github/workflows/ci.yml` |
| Release orchestration | `.github/workflows/build-release.yml` |
| Local release (KPatcher) | `scripts/publish_release.ps1` — **pubxml + `dist\<rid>.zip`**, differs from GHA zip names |
| Other product template | `scripts/publish_release_standalone.ps1` — default **KOTORModSync**, not KPatcher |

### CI vs local release (fifth pass summary)

| Aspect | GitHub Actions (`build-all-platforms.yml`) | `scripts/publish_release.ps1` |
|--------|---------------------------------------------|------------------------------|
| net9 command | Inline `dotnet publish` + `-p:PublishSingleFile` + `-p:IncludeNativeLibrariesForSelfExtract` + `-p:PublishReadyToRun` + `-o dist/build/...` | `dotnet publish` + **`PublishProfiles\*.pubxml`** |
| Zip name | `dist/KPatcher-<version>-<platform>-<arch>.zip` | **`dist\<rid>.zip`** (RID-based) |
| Version bump in csproj | Workflow step rewrites `<Version>` / `<AssemblyVersion>` / `<FileVersion>` | Script-driven / manual per script behavior |

**Product / compliance bullets for future plan sections**

1. **Signing:** Windows Authenticode (**SignTool** / Trusted Signing); macOS **codesign** + **notarization** for shipped GUI bundles; interaction with **single-file** extract + **NetSparkle** update checks.
2. **Release archive contract:** Zip root layout, `NOTICE`/licenses, naming **`KPatcher-$version-$platform-$arch.zip`** (see fourth pass — not always literal RID strings) vs script outputs.
3. **SBOM / redistribution:** CycloneDX/SPDX for self-contained drops; runtime redistribution terms; CVE triage for Avalonia/Skia stack.
4. **Version coupling:** Whether sidecar **FileVersion** must match KPatcher for merged-folder releases when workflows bump versions per-project.
5. **Linux packaging (optional):** AppImage/deb/Flatpak and whether sidecar CLIs go on **PATH** or sit next to the GUI only.

---

## Release hardening (best-practices-researcher)

### Code signing

- **Windows:** **SignTool** with explicit `/fd` and timestamp `/td` + `/tr` (RFC 3161); see [SignTool](https://learn.microsoft.com/windows/win32/seccrypto/signtool) and [Using SignTool to sign a file](https://learn.microsoft.com/windows/win32/seccrypto/using-signtool-to-sign-a-file). Cloud option: [Azure Trusted Signing](https://learn.microsoft.com/azure/trusted-signing/how-to-signing-integrations) and GitHub [`azure/artifact-signing-action`](https://github.com/azure/artifact-signing-action).
- **macOS:** Apple **Developer Program** workflow — signing + **notarization** for distribution outside the Mac App Store; CI on **macOS** runners ([MAUI Mac Catalyst deployment](https://learn.microsoft.com/dotnet/maui/mac-catalyst/deployment) / [publish outside App Store](https://learn.microsoft.com/dotnet/maui/mac-catalyst/deployment/publish-outside-app-store) — concepts apply to native/macOS bundles generally).
- **Secrets:** Keys via **GitHub Actions secrets** / federated identity; never in repo (pattern in Trusted Signing docs).

### GitHub Actions matrix + RID

- **Matrix jobs:** [`strategy.matrix`](https://docs.github.com/actions/using-jobs/using-a-matrix-for-your-jobs), [workflow syntax](https://docs.github.com/actions/reference/workflow-syntax-for-github-actions#jobsjob_idstrategymatrix).
- **.NET in Actions:** [Building and testing .NET](https://docs.github.com/actions/guides/building-and-testing-net) with **`actions/setup-dotnet`**.
- **Publish:** `dotnet publish -r <RID>` per [RID catalog](https://learn.microsoft.com/dotnet/core/rid-catalog), [deploying](https://learn.microsoft.com/dotnet/core/deploying/), [`dotnet publish`](https://learn.microsoft.com/dotnet/core/tools/dotnet-publish).

### SBOM and supply chain

- **NuGet Audit** at restore: [Auditing packages](https://learn.microsoft.com/nuget/concepts/auditing-packages), [NuGet security best practices](https://learn.microsoft.com/nuget/concepts/security-best-practices).
- **SBOM generation:** [microsoft/sbom-tool](https://github.com/microsoft/sbom-tool); GitHub [exporting an SBOM](https://docs.github.com/code-security/supply-chain-security/understanding-your-software-supply-chain/exporting-a-software-bill-of-materials-for-your-repository).
- **Dependency visibility:** [Dependency Submission API](https://docs.github.com/code-security/supply-chain-security/understanding-your-software-supply-chain/using-the-dependency-submission-api).
- **Attestations:** [Artifact attestations](https://docs.github.com/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations); [About supply chain security](https://docs.github.com/code-security/supply-chain-security/understanding-your-software-supply-chain/about-supply-chain-security).

---

## Linux desktop distribution notes (fourth pass)

- **`dotnet publish`** remains the supported packaging step for the published folder layout ([Deploying](https://learn.microsoft.com/dotnet/core/deploying/), [`dotnet publish`](https://learn.microsoft.com/dotnet/core/tools/dotnet-publish)).
- **`UseAppHost`:** Native launcher vs DLL-only; self-contained deployments expect the apphost ([MSBuild props — `UseAppHost`](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#useapphost)).
- **RIDs:** Prefer portable **`linux-x64`** / **`linux-arm64`** for glibc desktops; **`linux-musl-*`** for musl/Alpine ([RID catalog](https://learn.microsoft.com/dotnet/core/rid-catalog)).
- **Avalonia on Linux:** Native stacks (e.g. Skia) ship beside output; **FHS-style** packaging: `/usr/bin` launcher, `/usr/lib/<app>/` for publish tree, `/usr/share/applications/` + icons — see [Avalonia: Desktop Linux deployment](https://docs.avaloniaui.net/docs/deployment/linux).
- **`.desktop` entries:** [Desktop Entry Specification](https://specifications.freedesktop.org/desktop-entry-spec/desktop-entry-spec-latest.html) (`Exec`, `Icon`, `%F` / `%U`); Avalonia doc links examples.
- **Flatpak:** Often **`dotnet publish` with `--no-self-contained`** + **`org.freedesktop.Sdk.Extension.dotnet*`**; declare **sandbox permissions** (graphics, Wayland/X11) explicitly ([Flatpak: .NET](https://docs.flatpak.org/en/latest/dotnet.html), [Sandbox permissions](https://docs.flatpak.org/en/latest/sandbox-permissions.html)).
- **AppImage:** FUSE/extraction friction, glibc baseline, large self-contained payloads — see [AppImage troubleshooting](https://docs.appimage.org/user-guide/troubleshooting/) and [Bundling .NET Core apps](https://github.com/AppImage/AppImageKit/wiki/Bundling-.NET-Core-apps) (community wiki).
- **MSBuild `Copy` + globs:** Preserve subtrees with **`DestinationFolder` + `%(RecursiveDir)`** (or equivalent `DestinationFiles` transform); avoid **flattening** or defining recursive **`ItemGroup`** outside a target when files appear mid-build — [Copy task (recursive example)](https://learn.microsoft.com/visualstudio/msbuild/copy-task), [Well-known item metadata](https://learn.microsoft.com/visualstudio/msbuild/msbuild-well-known-item-metadata).

---

## Original plan content (preserved in spirit)

The base plan defines:

- **Layering:** `KPatcher.Core`, `KCompiler.Core`, `KPatcher.UI` (library), `KPatcher` (exe host), `KCompiler.NET`, `NCSDecomp.Core` / `NCSDecomp.NET`, `KEditChanges` + `KEditChanges.NET`.
- **Anti-patterns:** no `KPatcher.UI` → `KPatcher` or `KCompiler.NET` project references.
- **CLIs:** HoloPatcher-style KPatcher CLI; `kcompiler`; `NCSDecompCLI`; `--help` parity; solution membership; publish bundle; umbrella `keditchanges-cli`.

---

## Why there are several folders

### Research insights

**Best practices**

- **Separate assemblies** for domain logic (`*.Core`), UI (`*.UI`), and hosts (`*.NET` / app project) match .NET guidance for testability, reuse (e.g. NuGet `KPatcher.UI`), and clear dependency direction (UI → Core, never Core → UI).
- Microsoft’s **libraries tutorial** describes factoring logic into a core project with thin entry facades — same idea as Core / UI / CLI ([Develop libraries with the .NET CLI](https://learn.microsoft.com/dotnet/core/tutorials/libraries)).

**Performance**

- Irrelevant for this structural split; publish size is dominated by Avalonia + Skia for KPatcher, not project count.

**Edge cases**

- **Multi-targeting:** If `KPatcher` builds `net48` + `net9.0`, bundle target should stay **conditional** on TFM versions that **KCompiler.NET** / **NCSDecomp.NET** actually support.

---

## Headless CLIs

### Research insights

**Best practices**

- **Single responsibility** per executable: easier packaging, scripting, and KOTORModSync-style integration (explicit binary + args).
- **Shared library API** for duplicate CLIs (`NcsDecompCli.Run`, `ManagedNwnnsscomp` + `NwnnsscompCliParser`) avoids drift between standalone and umbrella tools.
- **Apphost:** For framework-dependent deploys, prefer running the **app host** when possible ([Deploy .NET apps](https://learn.microsoft.com/dotnet/core/deploying/deploy-with-cli)).

**HoloPatcher parity**

- Match **flags** and **positional** parsing with [vendor/PyKotor/Tools/HoloPatcher/src/holopatcher/core.py](../../vendor/PyKotor/Tools/HoloPatcher/src/holopatcher/core.py).
- Python uses `parse_known_args`; C# may reject unknown tokens — document or align if mod tools pass extra args.

**Edge cases**

- **`--help` with other flags:** argparse prints help and exits; ensure **help-first** exit order if strict parity is required.

---

## Publish bundling (`PublishBundledCliTools`)

### Research insights

**Best practices**

- **`dotnet publish` to the same folder twice** often **replaces** output from the first publish. Staging under `obj/.../sidecar_*` (paths rooted at the **KPatcher** project) then **Copy** with `DestinationFiles` transform preserves KPatcher’s layout.
- **Solution-level `-o`:** .NET 7+ disallows `--output` for solution publish because merged outputs are ambiguous ([breaking change](https://learn.microsoft.com/dotnet/core/compatibility/sdk/7.0/solution-level-output-no-longer-valid)) — reinforces **per-project** `PublishDir` + explicit merge (this repo’s pattern).
- **Class libraries:** Should not publish; use `IsPublishable=false` where applicable ([`dotnet publish` / MSBuild props](https://learn.microsoft.com/dotnet/core/tools/dotnet-publish)).
- **MSBuild extension:** Use `AfterTargets="Publish"` (or documented `AfterPublish` / `PublishBuildDependsOn`) rather than redefining built-in targets ([Extend the build process](https://learn.microsoft.com/visualstudio/msbuild/how-to-extend-the-visual-studio-build-process?view=vs-2022)).

**Implementation detail (conceptual)**

- Use **absolute paths** for **staging** `PublishDir` when invoking nested `MSBuild` `Publish` (this repo: `NormalizePath`/`EnsureTrailingSlash` under KPatcher’s `obj/.../sidecar_*`), so children do not resolve relative paths under **their** project dir. Final merge into KPatcher’s `PublishDir` is a separate **`Copy`** step — do not pass the **final** merge folder as each child’s `PublishDir` or the first publish output would be wiped by the second child publish if they shared it.

**CI / quality**

- Add a non-flaky check: after `dotnet publish … -f net9.0`, assert `kcompiler` / `NCSDecompCLI` apphosts exist (platform-specific extensions).

**References**

- [.NET application publishing](https://learn.microsoft.com/dotnet/core/deploying/)
- [`dotnet publish`](https://learn.microsoft.com/dotnet/core/tools/dotnet-publish)

---

## KEditChanges umbrella (`keditchanges-cli`)

### Research insights

**Best practices**

- **Subcommand router** in one exe is standard (`git`-style). Keep **thin**: delegate to libraries, not to other exe projects (avoids duplicate `Main` and ambiguous references).

**SDK / NuGet**

- **Ambiguous project name** errors (observed with assembly identity `keditchanges`) can appear on restore when names collide in the graph; hyphenated or distinct `AssemblyName` / project file name reduces risk.

**Single-file**

- **Always RID-specific** when publishing single-file; watch **native library** extraction, **`Assembly.Location`**, and **`DOTNET_BUNDLE_EXTRACT_BASE_DIR`** for headless/CI environments ([Single-file deployment](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview)).
- `PublishSingleFile` in **Release** for `KEditChanges.NET` — measure startup vs size; `EnableCompressionInSingleFile` trades disk for CPU at startup.

**Tools vs folder deploy**

- **`dotnet tool`:** NuGet-delivered, user-local PATH; good for developer machines ([.NET tools](https://learn.microsoft.com/dotnet/core/tools/global-tools)).
- **Sidecar folder (this repo’s KPatcher publish):** xcopy-friendly layout next to GUI; distinct from global tools ([Publishing modes](https://learn.microsoft.com/dotnet/core/deploying/deploy-with-cli)).

---

## Documentation

### Research insights

- **AGENTS.md** is the right place for contributor commands; keep the **“Which binary do I run?”** table updated when new entry points ship.
- Optional: one-line pointer in **README** for end users who only download releases.

---

## Appendix — Microsoft Learn URL index

| Topic | URL |
|--------|-----|
| Publishing modes (FDD vs self-contained) | https://learn.microsoft.com/dotnet/core/deploying/deploy-with-cli |
| Deploying hub | https://learn.microsoft.com/dotnet/core/deploying/ |
| `dotnet publish` / `PublishDir` / `IsPublishable` | https://learn.microsoft.com/dotnet/core/tools/dotnet-publish |
| Solution `--output` breaking change (.NET 7+) | https://learn.microsoft.com/dotnet/core/compatibility/sdk/7.0/solution-level-output-no-longer-valid |
| MSBuild `AfterTargets` / extend publish | https://learn.microsoft.com/visualstudio/msbuild/how-to-extend-the-visual-studio-build-process?view=vs-2022 |
| Artifacts output layout (.NET 8+) | https://learn.microsoft.com/dotnet/core/sdk/artifacts-output |
| Core library + solution structure | https://learn.microsoft.com/dotnet/core/tutorials/libraries |
| Project / folder organization | https://learn.microsoft.com/dotnet/core/porting/project-structure |
| .NET tools (global/local) | https://learn.microsoft.com/dotnet/core/tools/global-tools |
| RID-specific / self-contained / AOT tools (.NET 10+) | https://learn.microsoft.com/dotnet/core/tools/rid-specific-tools |
| Single-file overview | https://learn.microsoft.com/dotnet/core/deploying/single-file/overview |
| Trimming (self-contained) | https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-self-contained |
| MSBuild common properties | https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props |
| SignTool (Windows) | https://learn.microsoft.com/windows/win32/seccrypto/signtool |
| Using SignTool to sign a file | https://learn.microsoft.com/windows/win32/seccrypto/using-signtool-to-sign-a-file |
| Azure Trusted Signing integrations | https://learn.microsoft.com/azure/trusted-signing/how-to-signing-integrations |
| GitHub Actions matrix jobs | https://docs.github.com/actions/using-jobs/using-a-matrix-for-your-jobs |
| Building and testing .NET on Actions | https://docs.github.com/actions/guides/building-and-testing-net |
| SBOM tool (Microsoft) | https://github.com/microsoft/sbom-tool |
| GitHub artifact attestations | https://docs.github.com/actions/how-tos/secure-your-work/use-artifact-attestations/use-artifact-attestations |
| Avalonia — Linux deployment | https://docs.avaloniaui.net/docs/deployment/linux |
| Desktop Entry Specification | https://specifications.freedesktop.org/desktop-entry-spec/desktop-entry-spec-latest.html |
| Flatpak — .NET | https://docs.flatpak.org/en/latest/dotnet.html |
| Flatpak — sandbox permissions | https://docs.flatpak.org/en/latest/sandbox-permissions.html |
| AppImage — troubleshooting | https://docs.appimage.org/user-guide/troubleshooting/ |
| MSBuild `Copy` task | https://learn.microsoft.com/visualstudio/msbuild/copy-task |
| MSBuild well-known item metadata (`RecursiveDir`) | https://learn.microsoft.com/visualstudio/msbuild/msbuild-well-known-item-metadata |
| ReadyToRun deployment | https://learn.microsoft.com/dotnet/core/deploying/ready-to-run |
| Single-file deployment (extraction, bundling hooks) | https://learn.microsoft.com/dotnet/core/deploying/single-file/overview |
| Publish .NET apps for macOS | https://learn.microsoft.com/dotnet/core/deploying/macos |
| Microsoft Defender SmartScreen | https://learn.microsoft.com/windows/security/operating-system-security/virus-and-threat-protection/microsoft-defender-smartscreen/ |
| dotnet/reproducible-builds (README) | https://github.com/dotnet/reproducible-builds/blob/master/README.md |
| Apple — Notarizing macOS software | https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution |

---

## Suggested follow-up work (post–base plan)

**Done (2026-03-20 — `/work` + compound, this session):** **#12** (**`/t:Publish`** net48 in **`build-all-platforms`** + **`test-builds`**), **#15** (**`test-builds`** net48 **`PublishDir`** = release-style publish). **Compound:** [`deployment-issues/msbuild-publishdir-requires-publish-target.md`](../solutions/deployment-issues/msbuild-publishdir-requires-publish-target.md).  
**Done (earlier `/work` + deepen/compound):** #7, #10, #14, #16, #17, **#19** (`--help` smoke); **#12** shell half; **Compound:** lexer pushback, **`gha-pwsh-shell-syntax-mismatch.md`**, **`kpatcher-publish-bundled-cli-tools-merge.md`**.

1. **CI publish smoke** (artifact contents, optional matrix of RIDs).
2. **Parity audit:** unknown args, help precedence vs. HoloPatcher.
3. **Optional:** post-publish rename to `keditchanges.exe` if branding requires it.
4. **Publish matrix doc** when `net10.0` lands on KPatcher or sidecars.
5. **Solution folder cleanup** (`src` orphan in `.sln`).
6. **KEditChanges** library: real ChangeEdit-facing commands when RE/parity lands.
7. ~~**Workflow verify step:** assert `kcompiler` + `NCSDecompCLI` apphosts after `net9.0` publish (mirror `build-all-platforms` / `test-builds`).~~ **Done**
8. **Document net48** zip contents (no bundled sidecars) vs **net9.0** merged layout.
9. **Release hardening** subsection in release runbook: signing, SBOM, archive contract (see **Release hardening** above).
10. ~~**AGENTS.md:** add the **canonical** net9 publish line (fourth pass) next to the minimal FDD example; document **zip** naming (`platform`-`arch` vs RID).~~ **Done**
11. **Optional:** publish **`NCSDecomp.UI`** / **`keditchanges-cli`** as separate workflow artifacts if releases should ship them pre-built.
12. ~~**CI fix or doc:** net48 **`msbuild`** with **`PublishDir`** must use **`/t:Publish`**; **`shell: pwsh`** + bash **`if`** was fixed earlier.~~ **Done** (eleventh pass — both workflows; see compound doc).
13. **Optional:** align **`publish_release.ps1`** zip naming with GHA (`KPatcher-<ver>-<platform>-<arch>.zip`) or document “local RID zip” as intentional.
14. ~~**Policy/docs:** **`test-builds.yml`** — document or remove **`continue-on-error: true`** on the test step so “green workflow” matches test health.~~ **Commented**
15. ~~**Optional CI alignment:** **`test-builds`** net48 — same **`msbuild /t:Publish` + `PublishDir`** as **`build-all-platforms`**~~ **Done** (eleventh pass)
16. ~~**AGENTS.md:** one line listing **expected bundled apphost basenames** per OS (**`kcompiler`**, **`NCSDecompCLI`**, `.exe` on Windows) next to bundle docs — complements #7.~~ **Done**
17. ~~**One-time:** validate **`build-all-platforms`** Build step (**bash `if` + `pwsh`**) on **`windows-latest`** logs / local replay.~~ **Obviated**
18. **Supply chain:** enable **dependency review** on PRs (playbook: [`github-actions-dependency-review-dotnet.md`](../solutions/deployment-issues/github-actions-dependency-review-dotnet.md)); **`actions/attest`** release zips (+ optional **SBOM** via [sbom-tool](https://github.com/microsoft/sbom-tool)); cross-link release runbook (#9).
19. ~~**Optional:** **`--help` smoke** from **`PublishDir`** for each apphost in CI~~ **Done** (net48: **KPatcher** only; net9: main + sidecars; **`chmod +x`** on non-Windows).

---

## Post–deepen options

1. **View diff** — `git diff docs/plans/2026-03-21-cli-layout-bundling-deepened.md`
2. **Implement** follow-ups via `/workflows:work` or tracked issues
3. **Deepen further** — e.g. security review of `dotnet tool` trust model for KCompiler global tool distribution
4. **Revert** — restore prior version of this file from git if needed
