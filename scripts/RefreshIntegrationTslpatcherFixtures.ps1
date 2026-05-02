# Maintainer helper: refresh anonymous TSLPatcher trees under tests/KPatcher.Tests/EmbeddedIntegrationMods/.
# Copy or extract any two representative mods into:
#   scenario_a/   (expects tslpatchdata/changes.ini, changes_vanillaseq.ini, namespaces.ini, ...)
#   scenario_b/   (expects tslpatchdata/changes_16x9.ini, changes_4x3.ini, namespaces.ini, ...)
# Trim shipping README/INSTALL/Script Source if present so the repo only carries patcher inputs.
# After replacing fixtures, run TslpatcherIntegrationModTests and adjust counts in tests if INIs change.
$ErrorActionPreference = 'Stop'
