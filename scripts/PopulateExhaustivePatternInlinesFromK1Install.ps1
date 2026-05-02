# Maintainer helper: run one or all KotOR I → exhaustive_pattern_inlines module copy scripts.
# K1GameRoot may be a minimal maintainer bootstrap tree as long as it includes the module capsules these helpers read.
# Does not copy Override/mesh assets from mods; those remain manual per-bundle (see docs/TESTING.md).
param(
    [Parameter(Mandatory = $true)]
    [string] $K1GameRoot,
    [ValidateSet('All', 'Kor', 'Namespace', 'GffGit')]
    [string] $Bundle = 'All'
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

function Invoke-KorPayload {
    & (Join-Path $here 'PopulateMultiOptionKorGffBundleFromK1Install.ps1') -K1GameRoot $K1GameRoot
}

function Invoke-NamespacePayload {
    & (Join-Path $here 'PopulateNamespaceMainAltGffBundleFromK1Install.ps1') -K1GameRoot $K1GameRoot
}

function Invoke-GffGitPayload {
    & (Join-Path $here 'PopulateGffGitModuleTextureBundleFromK1Install.ps1') -K1GameRoot $K1GameRoot
}

switch ($Bundle) {
    'All' {
        Invoke-KorPayload
        Invoke-NamespacePayload
        Invoke-GffGitPayload
    }
    'Kor' { Invoke-KorPayload }
    'Namespace' { Invoke-NamespacePayload }
    'GffGit' { Invoke-GffGitPayload }
}

Write-Host "Done (Bundle=$Bundle)."
