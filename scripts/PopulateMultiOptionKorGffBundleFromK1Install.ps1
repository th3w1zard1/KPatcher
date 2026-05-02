# Maintainer helper: copy KotOR I module capsules into the exhaustive inline fixture so
# MultiOptionKorGffBundleExhaustiveInstallTests (KorExhaustiveBinaryFixtures.runsettings) can run against real bytes.
# Omit *.exe from the mod when refreshing tslpatchdata; this script only supplies vanilla-only sources.
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

$payloadRoot = Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\exhaustive_pattern_inlines\multi_option_kor_gff_bundle\tslpatchdata'
$srcModules = Resolve-K1ModulesPath -GameRoot $K1GameRoot

$names = @(
    'manm26ad.mod',
    'korr_m33aa.mod',
    'korr_m33ab.mod',
    'korr_m34aa.mod',
    'korr_m35aa.mod',
    'korr_m36aa.mod',
    'korr_m37aa.mod',
    'korr_m38ab.mod',
    'korr_m39aa.mod'
)

New-Item -ItemType Directory -Force -Path $payloadRoot | Out-Null

foreach ($n in $names) {
    $src = Join-Path $srcModules $n
    $dest = Join-Path $payloadRoot $n
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination $dest -Force
        continue
    }

    if (-not (Test-Path -LiteralPath $dest -PathType Leaf)) {
        throw "Missing packaged module and no bootstrap fallback found: $dest"
    }

    Write-Host "Keeping packaged module bytes for $n"
}

$iniPaths = Get-ChildItem -LiteralPath $payloadRoot -Filter '*.ini' -File |
    Where-Object { $_.Name -ne 'namespaces.ini' } |
    Select-Object -ExpandProperty FullName

& (Join-Path $here 'PopulateExhaustiveInlineVanillaSourcesFromK1Install.ps1') -K1GameRoot $K1GameRoot -IniPaths $iniPaths

Write-Host "Copied $($names.Count) .mod files to $payloadRoot"
Write-Host 'If changes.ini references install_folder1 under modules\korr_m37aa.mod, mirror that layout from the packaged mod (may differ from retail Modules layout).'
