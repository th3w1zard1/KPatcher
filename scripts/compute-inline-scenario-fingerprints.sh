#!/usr/bin/env bash
# Compute SHA-256 game-tree fingerprints for inline embedded install scenarios.
# Usage: ./scripts/compute-inline-scenario-fingerprints.sh [KPatcher.sln]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SLN="${1:-KPatcher.sln}"

export KP_CAPTURE_SCENARIO_GOLDENS=1
bash "$ROOT/scripts/dotnet-test.sh" "$ROOT/tests/KPatcher.Tests/KPatcher.Tests.csproj" -c Debug \
  --filter "FullyQualifiedName~ScenarioGoldenCaptureTests"
