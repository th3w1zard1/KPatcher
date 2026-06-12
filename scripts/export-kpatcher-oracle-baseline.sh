#!/usr/bin/env bash
# Export a KPatcher install manifest baseline for manual diff against TSLPatcher.exe output.
# Usage: ./scripts/export-kpatcher-oracle-baseline.sh [scenario_id] [output_file]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCENARIO_ID="${1:-inline_install_marker}"
OUTPUT="${2:-./oracle-baseline-${SCENARIO_ID}.txt}"

export KP_EXPORT_ORACLE_BASELINE=1
export KP_EXPORT_ORACLE_SCENARIO_ID="$SCENARIO_ID"
export KP_EXPORT_ORACLE_BASELINE_PATH="$OUTPUT"

bash "$ROOT/scripts/dotnet-test.sh" "$ROOT/tests/KPatcher.Tests/KPatcher.Tests.csproj" -c Debug \
  --filter "FullyQualifiedName~ScenarioGoldenCaptureTests.ExportSingleScenarioBaseline_WhenEnvSet"

if [[ -f "$OUTPUT" ]]; then
  echo "Baseline written: $OUTPUT"
  echo "Set KPATCHER_ORACLE_MANIFEST_BASELINE=$OUTPUT for ModInstallerOracleReferenceTests"
else
  echo "Export failed — check KP_EXPORT_ORACLE_* env and scenario id." >&2
  exit 1
fi
