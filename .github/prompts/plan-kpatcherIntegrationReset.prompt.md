## Plan: KPatcher Integration Suite Reset

Reset `KPatcher.Tests` around three explicit lanes instead of continuing the current mixed integration surface:

1. `engine/format regression`
2. `real extracted-mod end-to-end`
3. `scenario/archive-corpus regression`

The extracted `k1` and `tsl` trees remain on disk as the current source of truth. Active real-mod tests must, at runtime, write the selected `changes.ini` and any required `namespaces.ini` into temporary `tslpatchdata` roots, build heuristic-faithful temporary K1 or TSL install trees, run `ConfigReader`, run `ModInstaller`, and then assert strict post-install outcomes.

This reset prioritizes correctness, deterministic execution, and ownership clarity over minimizing churn.

## Goal

Produce a test layout where:

1. every active integration file belongs to one clear lane,
2. every extracted mod has a deterministic path to strict end-to-end coverage,
3. no active real-mod suite depends on the old projected-fixture output-copy contract, and
4. legacy smoke shells stop obscuring what is and is not actually verified.

## Verified Current State

### Corpus facts

- The extracted real-mod corpus lives at:
	- `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/extracted/k1`
	- `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/extracted/tsl`
- The currently known extracted-root count is `112` total:
	- `63` K1 roots
	- `49` TSL roots
- The current projected-fixture runtime path is still centered on:
	- `tests/KPatcher.Tests/Integration/RealModProjectedTestFixtures.cs`
	- `tests/KPatcher.Tests/Integration/RealModProjectedIntegrationTests.cs`
	- `tests/KPatcher.Tests/KPatcher.Tests.csproj`

### Coverage facts

- `tests/KPatcher.Tests/Integration/RealModExhaustiveCoverageRegistry.cs` currently tracks only a subset of dedicated exhaustive tests.
- The extracted corpus is not uniformly covered by strict end-to-end assertions.
- Existing gold-standard tests already define the target quality bar:
	- `tests/KPatcher.Tests/Integration/K1P004JuhaniRomanceEnhancementTests.cs`
	- `tests/KPatcher.Tests/Integration/K1P007SenniVekModTests.cs`
	- `tests/KPatcher.Tests/Integration/TslP024DisableDroidInterfaceFeatTests.cs`
	- `tests/KPatcher.Tests/Integration/TslP026HonestMerchantTests.cs`
	- `tests/KPatcher.Tests/Integration/TslP035RepairAffectsStunDroidTests.cs`
	- `tests/KPatcher.Tests/Integration/K1P056RepairAffectsStunDroidTests.cs`

### Heuristics facts

The synthetic game builders must align with actual checks in `src/KPatcher.Core/Tools/Heuristics.cs`, not merely create a minimal executable plus `dialog.tlk`.

Verified K1 PC heuristic probes:

- `streamwaves`
- `swkotor.exe`
- `swkotor.ini`
- `rims`
- `utils`
- `32370_install.vdf`
- `miles/mssds3d.m3d`
- `miles/msssoft.m3d`
- `data/party.bif`
- `data/player.bif`
- `modules/global.mod`
- `modules/legal.mod`
- `modules/mainmenu.mod`

Verified TSL PC heuristic probes:

- `streamvoice`
- `swkotor2.exe`
- `swkotor2.ini`
- `LocalVault`
- `LocalVault/test.bic`
- `LocalVault/testold.bic`
- `miles/binkawin.asi`
- `miles/mssds3d.flt`
- `miles/mssdolby.flt`
- `miles/mssogg.asi`
- `data/Dialogs.bif`

## Hard Requirements

### Functional requirements

- Every active real-mod test must perform `ConfigReader` loading against a runtime-written selected `INI` file.
- Every active real-mod test must execute `ModInstaller.Install` against a synthetic install tree.
- Every active real-mod test must assert post-install outcomes explicitly.
- Every extracted mod must end up in exactly one tracked coverage state:
	- `DedicatedStrict`
	- `ManifestStrict`
	- `ManifestSmokeTemporary`
	- `Blocked`
	- `OutOfScope`

### Structural requirements

- Every file under `tests/KPatcher.Tests/Integration` must belong to exactly one lane.
- Placeholder and duplicate `K1Pxxx` and `TslPxxx` shells must not remain once equivalent strict coverage exists.
- The active real-mod lane must not depend on `test_files/integration_tslpatcher_mods/fixtures` being copied to output.
- The scenario/archive-corpus lane may continue to use different corpus sources, but that ownership must be explicit.

### Data-handling requirements

- The extracted corpus remains committed and readable from disk for now.
- Runtime temp `tslpatchdata` trees must at least write the selected `changes.ini` and any selected `namespaces.ini` branch files.
- Payload copying from extracted roots must be selective and deterministic, not whole-tree copying by default.
- Identifying-info cleanup is not the primary blocking concern in this reset unless it affects runtime-written `INI` text or newly introduced descriptor metadata.

### Failure policy

- The real-mod harness must fail closed when required install-time sources or required game targets are missing.
- Placeholder stubbing is transitional only and must be explicitly tracked.
- Temporary compatibility exceptions must be enumerable and removable before sign-off.

## Non-Goals

- Fully eliminating all committed extracted mod trees in this reset.
- Rewriting production patcher behavior unrelated to test harness stability.
- Preserving every legacy file layout if it obscures lane ownership.
- Solving every historical fixture-policy issue before correctness and coverage are restored.

## Success Criteria

The reset is successful only when all of the following are true:

1. `Integration` is organized into clear lanes with no ambiguous ownership.
2. `RealModProjectedIntegrationTests` and `RealModProjectedTestFixtures` are either replaced or reduced to wrappers over the new extracted-corpus model.
3. `StrictFixtureBuilder` produces heuristic-faithful K1 and TSL temp installs.
4. Every extracted mod has a known strict-coverage state.
5. The active real-mod lane no longer depends on copied projected fixtures.
6. Engine/format regression tests still protect low-level behavior.
7. Scenario/archive-corpus tests remain available but no longer blur the meaning of real extracted-mod coverage.

## Guiding Principles

### Lane purity

If a file is about low-level patch-engine behavior, it belongs in the engine lane. If it exists to verify extracted mod corpus behavior, it belongs in the real-mod lane. If it exists for `scenario_patterns` or archive-corpus regression, it belongs in the corpus lane.

### Deterministic inputs

The harness must always know exactly which extracted root, selected `INI`, namespace branch, synthetic-game seed routine, and post-install assertions belong to a case.

### Minimal magic

Manifest-driven coverage is acceptable, but per-mod behavior cannot dissolve into opaque shared logic. The descriptor model must remain inspectable.

### Strictness first

Smoke-only rows are transitional, not the target state.

## Operational Invariants

These invariants are non-negotiable during execution of the reset:

1. The active real-mod lane must never infer the selected `INI` by scanning a directory and picking the first match.
2. The active real-mod lane must never copy an entire extracted mod tree unless a descriptor explicitly documents why selective copying is insufficient.
3. A real-mod test must not claim strict coverage if it lacks both `ConfigReader` assertions and post-install assertions.
4. A file may move lanes, but its behavior and purpose must remain legible after the move.
5. Placeholder stubs are allowed only as a short-lived migration exception and must be traceable to a concrete blocker entry.
6. No deletion of placeholder shells or generated fixture scaffolding may occur before an equivalent strict replacement exists and is discoverable.
7. The final lane split must be understandable from the repository tree and from test explorer output.

## Target Architecture

### Lane layout

The target shape should converge toward this split, whether through folders, namespaces, categories, or a combination of all three:

```text
tests/KPatcher.Tests/Integration/
	Engine/
	RealMods/
		Harness/
		Dedicated/
		Manifest/
	Corpus/
```

The exact folder split is negotiable. The semantics are not.

### Harness layers

The real-mod lane should have six clear layers:

1. source resolution
2. temp mod materialization
3. synthetic game creation
4. config verification
5. install execution
6. post-install verification

## Expected New Artifacts

The reset should converge on a small set of explicit artifacts rather than implicit behaviors scattered across existing helpers.

### Real-mod harness artifacts

Suggested new or replacement artifacts:

- `ExtractedModDescriptor.cs`
- `ExtractedModCoverageState.cs`
- `ExtractedModManifest.cs`
- `ExtractedModSourceResolver.cs`
- `ExtractedModInstallHarness.cs`
- `InstalledGameAssertions.cs`
- `SyntheticGameSeedBuilder.cs`
- `ExtractedModCoverageMatrix.md` or equivalent generated report

These may be introduced under a `RealMods/Harness` subtree or an equivalent namespace split.

### Reporting artifacts

The execution plan should produce machine-reviewable outputs for migration state, not just code changes.

Suggested outputs:

- `file disposition table`
- `coverage matrix`
- `blocker ledger`
- `deletion candidate list`
- `runtime source resolver contract`
- `strict batch migration report`

These outputs may live in planning docs, generated reports, or tracked markdown files, but they must exist as explicit artifacts.

## Proposed Descriptor Model

The real-mod lane should standardize around a descriptor shape that is explicit and inspectable.

### Suggested fields

- `CaseId`
- `SourceGame`
- `ExtractedRootRelativePath`
- `PrimaryIniRelativePath`
- `NamespaceConfigRelativePath`
- `SelectedNamespaceId`
- `ExpectedPatchShape`
- `PrepareSyntheticGame`
- `AssertConfigReader`
- `AssertInstalledGame`
- `AllowTemporaryStubbedTarget`
- `KnownBlocker`

### Descriptor rules

- A descriptor is invalid if it does not identify a specific selected `INI` path.
- A descriptor is invalid if it relies on unexplained implicit payload copying.
- A descriptor is invalid if it declares strict execution but contains no post-install assertions.
- A descriptor may temporarily carry a blocker note, but blocker notes must be enumerable.

### Descriptor extensions worth considering

The following fields are not mandatory at the start of the migration, but they are likely useful as the descriptor model expands:

- `ExpectedInstallListCount`
- `ExpectedGffPatchCount`
- `ExpectedTwoDAPatchCount`
- `ExpectedTlkPatchCount`
- `RequiresDialogTlkSeed`
- `RequiresModuleSeed`
- `RequiresOverrideSeed`
- `RequiresCompileListSupport`
- `RequiresHackListSupport`
- `ExpectedInstalledFiles`
- `ExpectedInstalledCapsuleResources`

These fields should be favored when they improve readability and reduce bespoke assertion code.

## File Disposition Strategy

### Keep and relocate

These files should survive, but with clearer lane ownership:

- `TLKIntegrationTests.cs`
- `SSFIntegrationTests.cs`
- `KPatcherTests.cs`
- `TwoDAIntegrationTests.cs`
- `TwoDAMemoryTests.cs`
- `TwoDACopyRowTests.cs`
- `TwoDAAdvancedTests.cs`
- `GFFIntegrationTests.cs`
- `GFFFieldTypeTests.cs`
- `GFFAdvancedTests.cs`
- `ModInstallerStrictIntegrationTests.cs`
- `ComprehensiveIntegrationTests.cs`
- `EdgeCaseIntegrationTests.cs`
- `SyntheticTslpatcherTemplatesWireformTests.cs`
- `TslpatcherPatternScenarioTests.cs`
- `TslpatcherPatternModInstallerTests.cs`
- `ExhaustivePatternInlineInstallTests.cs`
- `EmbeddedTslpatcherArchiveCorpus/*`

### Keep and refactor

These files are central but need semantic replacement or narrowing:

- `RealModProjectedIntegrationTests.cs`
- `RealModProjectedTestFixtures.cs`
- `ScenarioPatternModInstallHarness.cs`
- `StrictFixtureBuilder.cs`
- `RealModExhaustiveCoverageRegistry.cs`

### Keep as dedicated gold standards

- `K1P004JuhaniRomanceEnhancementTests.cs`
- `K1P007SenniVekModTests.cs`
- `TslP024DisableDroidInterfaceFeatTests.cs`
- `TslP026HonestMerchantTests.cs`
- `TslP035RepairAffectsStunDroidTests.cs`
- `K1P056RepairAffectsStunDroidTests.cs`

### Delete or replace

These should not survive in their current placeholder or obsolete form once equivalent coverage exists:

- placeholder `K1PxxxTests.cs` shells that only delegate to a generic harness without real fixture logic
- placeholder `TslPxxxTests.cs` shells that only delegate to a generic harness without real fixture logic
- generated projected-fixture-only code paths under `Integration/Generated` that no longer back an active suite
- `TslPatcherExeGoldenTests.cs` if its logic is not needed in the reset architecture
- `TslpatcherIntegrationModTests.cs` if its unique logic is extracted elsewhere
- `MultiOptionKorGffBundleExhaustiveInstallTests.cs` if its value is subsumed by the new descriptor model

### Deletion guardrails

No file should be deleted under this plan unless all of the following are true:

1. its replacement lane is identified,
2. its replacement logic exists,
3. the replacement logic is covered by at least one passing targeted verification step, and
4. any unique helper logic has either been extracted or intentionally discarded with rationale.

This is especially important for:

- placeholder `K1Pxxx` and `TslPxxx` shells,
- generated fixtures under `Integration/Generated`, and
- one-off integration utilities whose value is not obvious from filename alone.

## Synthetic Game Design

### K1 synthetic layout requirements

`StrictFixtureBuilder` must be able to create, at minimum, a credible K1 tree with:

- `swkotor.exe`
- `swkotor.ini`
- `dialog.tlk`
- `streamwaves/`
- `rims/`
- `utils/`
- `miles/mssds3d.m3d`
- `miles/msssoft.m3d`
- `data/party.bif`
- `data/player.bif`
- `modules/global.mod`
- `modules/legal.mod`
- `modules/mainmenu.mod`
- `Override/`

### TSL synthetic layout requirements

The TSL builder must be able to create, at minimum:

- `swkotor2.exe`
- `swkotor2.ini`
- `dialog.tlk`
- `streamvoice/`
- `LocalVault/`
- `LocalVault/test.bic`
- `LocalVault/testold.bic`
- `miles/binkawin.asi`
- `miles/mssds3d.flt`
- `miles/mssdolby.flt`
- `miles/mssogg.asi`
- `data/Dialogs.bif`
- `Modules/`

### Builder enhancements

The builders should provide helpers for:

- creating empty modules as real ERF capsules
- adding or replacing resources inside a capsule
- seeding baseline `2DA`, `GFF`, `TLK`, and `SSF` state
- copying selected extracted payloads into override or modules
- creating dialog roots and baseline area resources

## Runtime Directory Contract

The real-mod harness should create runtime temp directories using a deterministic structure so failures are inspectable and helper behavior is consistent.

Suggested temp layout:

```text
<tempRoot>/
	game/
	mod/
		selected-mod-root/
			tslpatchdata/
				changes.ini
				namespaces.ini
	logs/
	artifacts/
```

### Contract rules

1. `game/` contains the synthetic K1 or TSL install root.
2. `mod/selected-mod-root/tslpatchdata/` contains only the runtime-selected mod inputs required for the case.
3. `logs/` is reserved for diagnostic captures if the harness decides to persist them on failure.
4. `artifacts/` is reserved for temporary helper outputs such as generated manifests or diff snapshots.
5. The harness must be able to clean up this structure best-effort while still allowing investigation when configured to preserve failures.

## Real-Mod Materialization Algorithm

The active real-mod lane should follow one deterministic materialization algorithm.

### Materialization steps

1. Resolve the descriptor.
2. Resolve the extracted source root from the descriptor.
3. Resolve the selected `INI` and optional namespace branch from the descriptor.
4. Create the temp `mod` root and temp `tslpatchdata` root.
5. Write the selected `changes.ini` as a runtime file.
6. Write the selected `namespaces.ini` only if the case requires namespace resolution.
7. Copy only the payload files actually required by the selected branch.
8. Build the synthetic game root.
9. Seed the synthetic game according to the descriptor and shared builder helpers.
10. Run `ConfigReader` assertions.
11. Run `ModInstaller.Install`.
12. Run post-install assertions.

### Materialization anti-patterns

The plan should explicitly reject these behaviors:

- copying every file under an extracted mod root “just to be safe”
- relying on `AppContext.BaseDirectory` content copies when the source tree is already authoritative
- silently falling back to a different `INI` when the selected one is missing
- allowing a case to succeed because an install source was stubbed without the descriptor declaring that fact

## Real-Mod Migration Strategy

### Migration batches

Migrate extracted mods in explicit batches rather than trying to convert all `112` at once.

Recommended order:

1. `InstallList-only`
2. `2DA-heavy`
3. `TLK + override`
4. `module capsule + GFF`
5. `namespace/multi-option`
6. `NCS/NSS/CompileList/HACKList` heavy cases

### First strict batch

The first strict manifest-driven batch should include at least:

- one `InstallList-only` case
- one `2DA` patch case
- one `TLK` patch case
- one `module capsule + GFF` case
- one `namespace/multi-option` case

The purpose of the first batch is to prove the descriptor model and synthetic builders, not to maximize raw coverage immediately.

## Coverage Matrix Schema

The coverage matrix should be explicit and machine-readable. Each extracted root should map to one row with, at minimum, these columns:

- `CaseId`
- `Game`
- `ExtractedRootRelativePath`
- `PrimaryIniRelativePath`
- `HasNamespaces`
- `CoverageState`
- `LaneOwner`
- `SelectedBatch`
- `NeedsModuleSeed`
- `NeedsDialogTlkSeed`
- `NeedsOverrideSeed`
- `NeedsCompileSupport`
- `NeedsHackSupport`
- `HasDedicatedTest`
- `HasManifestStrictRow`
- `KnownBlocker`
- `Notes`

### Coverage matrix rules

1. Every extracted root gets exactly one row.
2. `CoverageState` values must come from the approved enum set.
3. `KnownBlocker` must be empty for `DedicatedStrict` and `ManifestStrict` rows.
4. Temporary smoke rows must state why they are not yet strict.
5. If a dedicated class and a manifest row both exist, one must be designated as the owner and the other must be categorized as redundant or supportive.

## ConfigReader Assertion Policy

Every strict real-mod test must assert `ConfigReader` output at the level appropriate to the mod. At minimum:

- selected `InstallList` count
- selected `2DA`, `GFF`, `TLK`, `SSF`, `NSS`, `NCS`, `CompileList`, and `HackList` counts where relevant
- key `SourceFile`, `Destination`, and `SaveAs` values
- namespace resolution when `namespaces.ini` is present

For gold-standard or complex cases, assertions should also verify critical modifier counts and modifier identities.

## Post-Install Assertion Policy

Strict real-mod tests must assert outputs at the level the mod actually changes. This may include:

- override file existence and exact bytes
- installed module existence
- capsule resource presence
- `GFF` field values
- `2DA` cell values
- `TLK` entry values or counts
- `SSF` slot values
- `NCS` byte equality or expected mutation markers
- install-log success phrases and absence of errors

“Install completes successfully” is not enough.

## Shared Assertion Helpers to Add

Suggested helpers:

- `InstalledGameAssertions.AssertOverrideFileBytes`
- `InstalledGameAssertions.AssertCapsuleContains`
- `InstalledGameAssertions.AssertCapsuleResourceBytes`
- `InstalledGameAssertions.AssertGffField`
- `InstalledGameAssertions.AssertTwoDACell`
- `InstalledGameAssertions.AssertTlkEntry`
- `InstalledGameAssertions.AssertInstallLogSuccess`

These helpers must remain thin wrappers over real assertions, not a second hidden harness layer.

## Branch and Namespace Handling Rules

Namespace and multi-option mods are a recurring source of ambiguity. The plan should explicitly define how they are selected and tested.

### Rules

1. A namespace-driven case must name the selected namespace branch explicitly.
2. A namespace-driven case must assert the namespace parse result before install execution.
3. A namespace-driven case must copy only the payloads relevant to the selected branch unless the branch semantics require shared assets.
4. Dedicated tests are preferred for namespace cases where multiple branches materially change seeding or post-install expectations.
5. Manifest-driven rows remain acceptable for namespace cases only if the branch-specific behavior is still readable from the descriptor.

## Manifest and Registry Strategy

### Manifest source

The new manifest should be derived from the extracted corpus and inventory metadata, not from the projected fixture output directory.

Possible inputs:

- `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/inventory/inventory.json`
- a new `ExtractedModManifest`

### Registry rules

`RealModExhaustiveCoverageRegistry` should either be updated or replaced so that it can distinguish:

- dedicated handwritten exhaustive coverage
- manifest-driven strict coverage
- temporary smoke coverage

The registry must not silently drift from actual test ownership.

## Build Health Strategy

The repository already has a failing `KPatcher.Tests` build in context. The plan should explicitly define how structural cleanup avoids compounding that failure.

### Rules

1. Lane reorganization should happen in buildable checkpoints.
2. When a file is moved or replaced, its compile-time dependencies must move or be replaced in the same checkpoint.
3. Placeholder-shell deletion must come after the replacement compiles.
4. The real-mod harness rewrite should be proven with a small strict batch before broad deletion or path-contract removal.

### Build checkpoints

Suggested checkpoints:

- `Checkpoint A`: lane directories or namespaces introduced with no semantic change.
- `Checkpoint B`: extracted-corpus resolver introduced and proven on one strict case.
- `Checkpoint C`: heuristic-faithful builders introduced and proven on K1 and TSL.
- `Checkpoint D`: first strict manifest batch green.
- `Checkpoint E`: projected-fixture output-copy path removed for the active real-mod lane.

## Project Wiring Changes

### Current project wiring to remove

The plan must eliminate active dependence on this project-content path once the real-mod suite is migrated:

- `test_files/integration_tslpatcher_mods/fixtures/**/*`

### Project wiring to re-evaluate

Re-evaluate whether these still need output-copy behavior after the lane split:

- `EmbeddedIntegrationMods/**/*`
- `test_files/exhaustive_pattern_inlines/**/*`

### Output cleanup

Delete or retire stale generated artifacts under:

- `tests/KPatcher.Tests/Integration/Generated`

only after no active lane depends on them.

## Risks

### Primary risks

- Reorganizing test files without restoring build health can create a long broken intermediate state.
- Tightening the synthetic builders may surface hidden assumptions in existing tests.
- Converting placeholder shells to strict descriptors may reveal that some extracted mods need more seed data than expected.
- The scenario/corpus lane may still contain utility logic that real-mod tests accidentally depend on today.

### Mitigations

- Keep buildable checkpoints after each lane-level reorganization.
- Migrate the first strict manifest batch before broad deletion of placeholder shells.
- Extract shared utilities deliberately instead of deleting legacy files blindly.
- Track temporary blockers explicitly rather than letting them become silent skips.

## Blocker Taxonomy

Not all blockers are equal. The plan should classify them so migration work is sequenced intelligently.

Suggested blocker categories:

- `MissingInstallSource`
- `MissingSyntheticGameSeed`
- `UnknownNamespaceSemantics`
- `UnsupportedCompileListShape`
- `UnsupportedHackListShape`
- `RetailStateDependencyUnknown`
- `AmbiguousPrimaryIni`
- `GeneratedFixtureDependency`

### Blocker handling rules

1. Every blocker must identify the affected `CaseId`.
2. Every blocker must have an owner lane and a next action.
3. Every blocker must specify whether it prevents `ManifestStrict`, `DedicatedStrict`, or both.
4. Temporary smoke coverage must reference blocker entries rather than freeform explanation.

## Decision Rules

When uncertain during execution:

1. prefer preserving test meaning over preserving file names,
2. prefer explicit per-mod descriptors over generic smoke rows,
3. prefer dedicated handwritten tests only where the descriptor model becomes harder to understand than a bespoke class,
4. prefer selective payload copying over whole-tree copying,
5. prefer failing closed over silent stubbing,
6. prefer lane separation over backward compatibility with the old mixed structure.

## Execution Phases

### Phase 1 - Freeze the target architecture

#### Objective

Define the three-lane target and lock the scope of the reset.

#### Tasks

1. Define the lane model for `Engine`, `RealMods`, and `Corpus`.
2. Confirm the extracted corpus remains on disk as the source of truth for this reset.
3. Confirm manifest-driven strict rows are the default shape for simpler mods.
4. Confirm dedicated handwritten classes remain only for genuinely bespoke cases.

#### Exit criteria

- no ambiguity remains about what “real extracted-mod integration” means

### Phase 2 - Build the context map and file disposition list

#### Objective

Assign every `Integration` file to a concrete disposition.

#### Tasks

1. Categorize each file as `KeepAndRelocate`, `KeepAndRefactor`, `Replace`, or `Delete`.
2. Enumerate all placeholder `K1Pxxx` and `TslPxxx` shells.
3. Identify which legacy helpers still provide unique logic.
4. Identify which scenario/corpus utilities are still coupled to the real-mod lane.

#### Exit criteria

- every `Integration` file has one explicit disposition

#### Required outputs

- file-by-file disposition table
- list of placeholder shell files
- list of helpers to extract before deletion

### Phase 3 - Replace the fixture source model for real mods

#### Objective

Stop treating projected fixtures as the active real-mod runtime source.

#### Tasks

1. Introduce a source resolver for extracted roots and inventory metadata.
2. Write selected `INI` and namespace files into temp `tslpatchdata` at runtime.
3. Selectively copy payload files referenced by the selected mod branch.
4. Replace or wrap projected-fixture assumptions.

#### Exit criteria

- at least one strict real-mod test runs without the projected-fixture output-copy path

#### Required outputs

- extracted source resolver contract
- temp materialization contract
- first strict real-mod descriptor

### Phase 4 - Rebuild the synthetic game installers around real heuristics

#### Objective

Make temp K1 and TSL installs reflect actual detection and install assumptions.

#### Tasks

1. Expand `StrictFixtureBuilder` for heuristic-faithful K1 trees.
2. Expand `StrictFixtureBuilder` for heuristic-faithful TSL trees.
3. Add composable seeding helpers.
4. Make required source and target failures explicit.

#### Exit criteria

- strict K1 and TSL temp trees satisfy the intended heuristic probes

#### Required outputs

- K1 synthetic layout helper contract
- TSL synthetic layout helper contract
- seed helper inventory

### Phase 5 - Standardize the end-to-end real-mod harness

#### Objective

Convert the real-mod lane to a descriptor-driven strict harness with reusable assertion helpers.

#### Tasks

1. Split or rename `ScenarioPatternModInstallHarness` responsibilities.
2. Introduce the descriptor model.
3. Add config and post-install assertion helpers.
4. Preserve gold-standard dedicated tests while enabling simpler manifest-driven strict rows.

#### Exit criteria

- one dedicated gold-standard case and one manifest-driven strict case both use the new semantics successfully

#### Required outputs

- descriptor type definition
- strict harness entry point
- shared assertion helper set

### Phase 6 - Migrate and fold the existing suites into the new structure

#### Objective

Re-home existing tests into the correct lanes and remove obsolete structures.

#### Tasks

1. Relocate engine/format tests.
2. Relocate scenario/corpus tests.
3. Replace projected-fixture-based real-mod tests.
4. Delete or replace placeholder shells.
5. Audit old one-off utilities.

#### Exit criteria

- no active test file remains in the wrong lane

#### Required outputs

- lane-aligned file layout
- deprecation list for obsolete files
- retained helper inventory

### Phase 7 - Fill real-mod coverage across the extracted corpus

#### Objective

Turn the extracted corpus into a tracked strict coverage program.

#### Tasks

1. Build the full coverage matrix.
2. Migrate by batch shape.
3. Add per-mod config assertions.
4. Add per-mod post-install assertions.
5. Track blockers explicitly.

#### Exit criteria

- every extracted root has a known coverage state

#### Required outputs

- coverage matrix
- blocker ledger
- strict batch completion report

### Phase 8 - Project wiring and repository cleanup

#### Objective

Remove obsolete runtime paths and stale generated artifacts.

#### Tasks

1. Remove `fixtures/**/*` content-copy dependence from `KPatcher.Tests.csproj`.
2. Re-evaluate `EmbeddedIntegrationMods` and `exhaustive_pattern_inlines` output-copy needs.
3. Delete stale generated artifacts only after active dependencies are gone.
4. Normalize folder names, namespaces, and categories.

#### Exit criteria

- the active real-mod lane no longer depends on projected fixtures being copied to output

#### Required outputs

- updated project wiring decision list
- generated-artifact removal list
- retained output-copy rationale for non-real-mod lanes

### Phase 9 - Verification and sign-off

#### Objective

Prove the new structure is correct, strict, and maintainable.

#### Tasks

1. Build `KPatcher.Tests` and restore build health.
2. Run targeted verification per lane.
3. Run a corpus-wide real-mod verification pass.
4. Verify synthetic-tree heuristic fidelity.
5. Confirm projected-fixture output-copy independence.

#### Exit criteria

- the new lane structure is buildable, understandable, and strictly testable

#### Required outputs

- lane-level verification summary
- corpus-wide coverage summary
- remaining blocker list, if any

## Relevant Files

- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/RealModProjectedIntegrationTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/RealModProjectedTestFixtures.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/ScenarioPatternModInstallHarness.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/StrictFixtureBuilder.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/RealModExhaustiveCoverageRegistry.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/K1P004JuhaniRomanceEnhancementTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/K1P007SenniVekModTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/TslP024DisableDroidInterfaceFeatTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/TslP026HonestMerchantTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/TslP035RepairAffectsStunDroidTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/K1P056RepairAffectsStunDroidTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/TslpatcherPatternScenarioTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/TslpatcherPatternModInstallerTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/ExhaustivePatternInlineInstallTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/Integration/TslpatcherIntegrationModTests.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/KPatcher.Tests.csproj`
- `c:/GitHub/KPatcher/src/KPatcher.Core/Tools/Heuristics.cs`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/extracted/k1`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/extracted/tsl`
- `c:/GitHub/KPatcher/tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/inventory/inventory.json`

## Verification Checklist

1. Every `Integration` file is assigned to one lane or explicitly deleted.
2. The new real-mod source model resolves extracted roots directly.
3. Runtime `INI` writing is used in the active real-mod lane.
4. Synthetic K1 trees satisfy intended K1 heuristic probes.
5. Synthetic TSL trees satisfy intended TSL heuristic probes.
6. Placeholder real-mod shells are removed once equivalent strict coverage exists.
7. The active real-mod lane does not require projected fixtures to be copied to output.
8. Engine/format and scenario/corpus lanes still pass targeted verification.

## Further Considerations

1. The first strict descriptor batch should be used to validate naming, helper boundaries, and the failure policy before broad migration.
2. If path resolution from source-tree roots proves cleaner than build-output resolution for all lanes, consider unifying on source-tree resolution where practical.
3. Generated projected-fixture artifacts should be treated as disposable migration scaffolding, not a long-term ownership model.
4. The registry may evolve into a richer coverage-reporting surface if strict manifest rows become the dominant form of ownership.
5. If the real-mod lane adopts source-tree resolution directly, the plan should explicitly state how test execution remains stable in IDE, CLI, and CI contexts.
6. If certain extracted mods remain blocked by unresolved retail-state dependencies, those blockers should feed back into improved synthetic game seed helpers rather than normalizing permanent smoke coverage.
