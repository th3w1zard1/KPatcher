Plan: Replace Corpus With Real Mod Suite

Replace the archive-corpus test path with a real downloaded-mod pipeline driven by portal-linked sources from the KOTOR Community Portal. The merged plan below combines discovery, download, strict 7-Zip-only extraction, per-archive inventory, anonymized fixture projection, and automated per-archive test generation.

Goal
- Replace the existing archive-corpus workflow with a pipeline that: (1) discovers portal-linked TSLPatcher/HoloPatcher mods, (2) downloads them (DeadlyStream-aware), (3) extracts them using 7-Zip only, (4) generates one anonymized unit test per mod (archive) that loads INI/YAML, verifies patch objects, runs ModInstaller, and asserts installed resources.

Constraints
- 7-Zip CLI only for listing/extraction.
- Keep raw downloads and full extracted trees in maintainer cache (gitignored).
- Commit only anonymized extracted fixtures safe for CI; preserve an offline provenance map for maintainers.
- Tests must be anonymized (opaque case IDs, removed author/branding strings where safe).
- Partition tests into `parser-only`, `installer-smoke`, and `exhaustive` tiers to bound default CI.

Phases (merged)
1) Source discovery & normalization
- Re-crawl K1 and TSL portal build pages with Firecrawl/Tavily and produce authoritative canonical manifests under `.firecrawl/` (e.g., `k1-full-links.json`, `k2-full-links.json`). Normalize duplicates and classify outbound hosts.

2) Source classification
- From the manifests derive structured records for each reachable archive entry: portal page, archive ID (when present), slug, host, canonical URL, and game classification. Identify DeadlyStream file pages, Dropbox/GitHub/Nexus links, and other hosts.

3) Download pipeline (maintainer cache)
- Feed the normalized manifest into `scripts/DownloadTslpatcherModsFromBuildPages.ps1` (reused and hardened). Keep DeadlyStream CSRF/session handling and Dropbox `?dl=1` rewriting. Place raw zips into `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/raw/` with deterministic `{id}-{slug}.zip` naming where possible.

4) Classify & 7-Zip-only extraction
- Use `scripts/OrganizeDeadlyStreamTslpatcherArchives.ps1` to `7z l` each archive and detect `tslpatchdata`/installer roots. Sort archives into `deadlystream_k1/`, `deadlystream_tsl/`, `deadlystream_cross/`, and `deadlystream_unlisted/`. Extract each archive with `7z.exe` into `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/extracted/<archive-id>/`. Log `7z` output and failures as structured JSON.

5) Inventory generation
- Walk each extracted archive root and emit an `inventory/<archive-id>.json` describing: entrypoint INI/YAML paths, namespace presence, counts by patch-section (2DA/GFF/SSF/NCS/TLK/InstallFile), Modules/*.mod list, Override paths, any executable binaries, and a deterministic opaque case ID (`k1_p000` / `tsl_p000` style). Include extraction logs and failure reasons.

6) Quarantine policy
- If an archive is missing `tslpatchdata`, has ambiguous multi-root layouts, or contains hostile/unsupported binaries, emit a quarantine record with an explicit reason and skip test generation. Keep quarantined archives in the inventory for provenance.

7) Anonymized fixture projection (committed)
- For supported archives, project a minimized anonymized extracted tree that retains only installer-relevant files (INI/YAML, tslpatchdata entries, Modules/*.mod, Override slices, and small binary payloads required for parser/installer validation). Strip readmes/license text and replace author/URL strings with placeholders. Store committed fixtures under `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/fixtures/<case-id>/` and do NOT commit raw downloads or full extracted trees.

8) Provenance mapping (maintainer-only)
- Keep a non-committed `provenance.json` mapping `case-id` → `{archiveId, originalFilename, sourceUrl, downloadedSha256, extractionLog}` to enable maintainers to rehydrate fixtures.

9) Parser & installer harness reuse
- Reuse existing harnesses and helpers:
  - Parser: `tests/KPatcher.Tests/Integration/ScenarioPatternTestFixtures.cs`, `src/KPatcher.Core/Reader/NamespaceReader.cs`, `src/KPatcher.Core/Reader/ConfigReader.cs`, `src/KPatcher.Core/Reader/ConfigReaderYaml.cs`, `src/KPatcher.Core/Config/PatcherConfig.cs`.
  - Installer: `tests/KPatcher.Tests/Integration/ScenarioPatternModInstallHarness.cs`, `tests/KPatcher.Tests/Integration/StrictFixtureBuilder.cs`, `src/KPatcher.Core/Patcher/ModInstaller.cs`.

10) Per-archive xUnit generation
- Generate one anonymized xUnit test per archive that:
  - Materializes the anonymized fixture into a temporary mod root via the harness materializer.
  - Runs parser assertions: namespace loading (if present), INI/YAML parse success, and patch-object counts matching `inventory`.
  - Optionally runs installer-smoke mode: pre-seed synthetic game roots, run `ModInstaller`, assert `StartingInstallation` and `InstallationCompletedSuccessfully`, and perform post-install file assertions (byte-equality or expected patch effects).
  - Mark tests with Traits to indicate `parser-only`, `installer-smoke`, or `exhaustive` so CI can partition them.

11) Runtime partitioning & CI
- Keep default CI small: include `parser-only` and a curated `installer-smoke` subset. Put full generated suite behind optional runsettings/workflows (e.g., `KorExhaustiveBinaryFixtures.runsettings` pattern) and gate heavy tiers via separate jobs or labels.

12) Archive-corpus replacement validation
- Validate that generated tests cover the parser, installer, and binary-surface expectations previously covered by the archive corpus. For edge cases that cannot be reproduced from real mods safely, create small focused inline tests.

13) Archive-corpus removal
- After validation, remove `tests/KPatcher.Tests/Integration/EmbeddedTslpatcherArchiveCorpus/` and `tests/KPatcher.Tests/test_files/integration_tslpatcher_archive_corpus/`, and update `tests/KPatcher.Tests/KPatcher.Tests.csproj` to drop content-copy entries for removed corpus files.

14) Docs & workflow updates
- Update `docs/TESTING.md`, `docs/INTEGRATION_TSLPATCHER_MODS.md`, and `docs/DEADLYSTREAM_CORPUS.md` to describe the new pipeline, anonymization policy, quarantine rules, provenance handling, and runsettings partitioning.

15) Verification gates
- Stable crawl counts and canonical manifests.
- Download integrity checks (no HTML masquerading as zip).`
- 7-Zip-only classification/extraction with deterministic logs.
- Inventory completeness with supported case IDs or quarantine reasons.
- Parser tests matching inventory expectations.
- Installer-smoke tests asserting `StartingInstallation` and `InstallationCompletedSuccessfully` and performing post-install checks.
- Default CI should exclude heavy generated tiers while exercising the replacement path.

16) Commit & maintenance
- Commit anonymized fixtures and generated tests, plus updated docs. Keep `provenance.json` and raw-extraction logs in maintainer-only storage (gitignored). Provide a maintainer README describing how to regenerate `.firecrawl` manifests and rebuild fixtures.

Decisions (locked)
- Commit anonymized extracted trees for CI (no runtime extraction from committed zips).
- One test per archive as the reporting unit.
- Unsupported/non-deterministic archives go into quarantine with explicit reasons (not failing generation).
- Raw downloads and full extracted trees remain maintainer-cache only (gitignored).
- Use `7z.exe` exclusively for archive listing and extraction.
- Remove the archive corpus as part of the same change after replacement validation.

Relevant files (for implementers)
- `tests/KPatcher.Tests/Integration/ScenarioPatternModInstallHarness.cs`
- `tests/KPatcher.Tests/Integration/StrictFixtureBuilder.cs`
- `tests/KPatcher.Tests/Integration/ScenarioPatternTestFixtures.cs`
- `tests/KPatcher.Tests/KPatcher.Tests.csproj`
- `tests/KPatcher.Tests/Default.runsettings` and `Exhaustive.runsettings`
- `scripts/DownloadTslpatcherModsFromBuildPages.ps1`
- `scripts/OrganizeDeadlyStreamTslpatcherArchives.ps1`
- `scripts/ExtractNeutralExhaustivePayloadFromZip.ps1`
- `.firecrawl/k1-full-links.json` and `.firecrawl/k2-full-links.json`

Next actions
- Phase 1: regenerate portal manifests via Firecrawl/Tavily and attach manifests for review.
- Phase 2: with approval, run the hardened downloader to populate the maintainer cache.
- Phase 3: run the 7-Zip extraction and emit the per-archive inventory.

Change log
- Merged both prior prompts into this single plan file (March 25, 2026).
