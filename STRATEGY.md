---
name: KPatcher
last_updated: 2026-05-23
---

# KPatcher Strategy

## Target problem

KotOR mods still depend on TSLPatcher-style installs, legacy script tooling, and behavior users expect to match long-standing Windows-centric workflows. That makes modern installs and maintenance brittle: players and maintainers need the same outcomes on current platforms without hand-patching game data or reverse-engineering every mod's patch logic.

## Our approach

Win by being the faithful C#/.NET port of KPatcher/TSLPatcher behavior, not a redesign: preserve compatibility first, then package that behavior in a modern cross-platform desktop app and managed CLI/tooling stack. That makes existing mod workflows usable on modern machines without breaking the assumptions mod authors and players already rely on.

## Who it's for

**Primary:** KotOR mod players installing TSLPatcher-style mods - They're hiring KPatcher to apply complex mod changes safely on a modern machine without hand-editing game files or keeping an old Windows-only toolchain alive.

**Secondary:** KotOR mod authors and tooling maintainers - They're hiring KPatcher and its libraries/CLIs to verify parity, debug patch behavior, and automate existing mod workflows with a managed toolchain.

## Key metrics

- **Default regression suite pass rate** - Pass/fail of `tests/KPatcher.Tests` plus the satellite suites in GitHub Actions.
- **Optional parity tier pass rate** - Pass/fail of exhaustive and golden-comparison workflows such as `test-optional-tiers.yml` and the explicit runsettings tiers.
- **Release adoption** - GitHub Releases download counts for KPatcher desktop builds and bundled CLI artifacts.
- **Library and tool uptake** - NuGet download counts for `KPatcher.Core`, `KPatcher.UI`, `KCompiler.Core`, and `KCompiler.NET`.
- **Post-release install/parity bug rate** - GitHub issues describing install regressions, parity gaps, or format/tool breakage after a release.

## Tracks

### Patcher parity

Keep install behavior, format handling, namespace/config parsing, and logging aligned with the Python and TSLPatcher baseline.

_Why it serves the approach:_ The product only wins if mods behave the same way the legacy tools and existing mod packages expect.

### Managed script and tooling coverage

Strengthen the built-in NSS/NCS compile-decompile stack and the bundled CLIs so the repo owns more of the workflow in managed code.

_Why it serves the approach:_ A faithful installer is more usable when the surrounding toolchain is modern, bundled, and cross-platform.

### Cross-platform desktop delivery

Ship KPatcher as a reliable Avalonia desktop app with publish, update, and packaging flows that work across supported runtimes.

_Why it serves the approach:_ Cross-platform packaging is how parity becomes usable for real players, not just library consumers.

### Regression and verification harness

Expand and maintain CI, vendor comparisons, runsettings tiers, and fixture policies that catch behavior drift before releases.

_Why it serves the approach:_ Parity is only credible if the repo keeps proving it release after release.

## Not working on

- Reinventing KPatcher away from 1:1 KPatcher/TSLPatcher behavior.
- Feature creep that adds novel workflows before compatibility and parity are solid.
- Depending on external compilers inside `NCSDecomp.Core`.

## Marketing

**One-liner:** A faithful, cross-platform KPatcher for modern KotOR mod installs.

**Key message:** KPatcher keeps the behavior mod users already trust, but brings it to a managed .NET/Avalonia stack with bundled tooling, cross-platform delivery, and a parity-first verification story.
