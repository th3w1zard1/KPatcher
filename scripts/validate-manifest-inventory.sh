#!/usr/bin/env bash
# Validate scenario_patterns/manifest.json via exhaustive-tier tests.
# Usage: ./scripts/validate-manifest-inventory.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

bash "$ROOT/scripts/dotnet-test.sh" "$ROOT/tests/KPatcher.Tests/KPatcher.Tests.csproj" -c Debug \
  --settings "$ROOT/tests/KPatcher.Tests/GeneratedGenericModExhaustive.runsettings"
