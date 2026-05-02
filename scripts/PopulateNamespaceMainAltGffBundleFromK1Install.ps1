# Maintainer helper: copy KotOR I module capsules referenced by namespace_main_alt_gff_bundle into the exhaustive fixture.
# Source files live next to each namespace's selected changes.ini under tslpatchdata/Main and tslpatchdata/Alternate.
param(
    [Parameter(Mandatory = $true)]
    [string] $K1GameRoot
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

function Resolve-K1ModulesPath {
    param([Parameter(Mandatory = $true)][string] $GameRoot)

    foreach ($relative in @('modules', 'Modules')) {
        $candidate = Join-Path $GameRoot $relative
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return $candidate
        }
    }

    throw "K1 modules folder not found under bootstrap root: $GameRoot"
}

$payloadRoot = Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\exhaustive_pattern_inlines\namespace_main_alt_gff_bundle\tslpatchdata'
$srcModules = Resolve-K1ModulesPath -GameRoot $K1GameRoot

$names = @(
    'korr_m35aa.mod',
    'korr_m36aa.mod',
    'lev_m40aa.mod',
    'lev_m40ad.mod',
    'manm26ad.mod',
    'manm27aa.mod',
    'sta_m45ac.mod',
    'tar_m02ad.mod',
    'tar_m04aa.mod'
)

$destinations = @(
    (Join-Path $payloadRoot 'Main'),
    (Join-Path $payloadRoot 'Alternate')
)

foreach ($destination in $destinations) {
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
}

foreach ($n in $names) {
    $src = Join-Path $srcModules $n
    foreach ($destination in $destinations) {
        $dest = Join-Path $destination $n
        if (Test-Path -LiteralPath $src) {
            Copy-Item -LiteralPath $src -Destination $dest -Force
            continue
        }

        if (-not (Test-Path -LiteralPath $dest -PathType Leaf)) {
            throw "Missing packaged module and no bootstrap fallback found: $dest"
        }

        Write-Host "Keeping packaged module bytes for $n in $(Split-Path -Leaf $destination)"
    }
}

$iniPaths = @(
    (Join-Path $payloadRoot 'Main\changes.ini'),
    (Join-Path $payloadRoot 'Alternate\changes.ini')
)

& (Join-Path $here 'PopulateExhaustiveInlineVanillaSourcesFromK1Install.ps1') -K1GameRoot $K1GameRoot -IniPaths $iniPaths

Write-Host "Copied $($names.Count) .mod files into namespace payload folders under $payloadRoot"
Write-Host 'Alternate/changes.ini InstallList (install_folder0=Override) expects: l_sithsoldier.mdl/.mdx, n_sithsoldier.mdl/.mdx, PLC_SSldCrps.tpc under tslpatchdata/Override/ — copy those from the packaged mod (not from retail K1).'
